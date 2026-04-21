using CommunityToolkit.Mvvm.Input;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utils;
using SilvaData.ViewModels;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace SilvaData.Controls
{
    public partial class ImagemSelecao : ContentView
    {
        // Propriedade interna para a Imagem do XAML (assumindo que 'cachedImage' é o x:Name)
        [Bindable(false)] // cachedImage é um Image, não BindableProperty
        private Image? CachedImageElement => this.FindByName<Image>("cachedImage");

        // O ImageChanged precisa do tipo correto no MAUI
        private static void ImageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ImagemSelecao)bindable;

            // Verifica se a imagem do XAML existe (ajuste o x:Name se necessário)
            var cachedImage = control.CachedImageElement;

            if (newValue is LoteFormImagem loteFormImagem && cachedImage != null)
            {
                var localImage = loteFormImagem.urlImagemLocal;

                // MUDANÇA MAUI: ImageSource.FromFile
                if (!string.IsNullOrWhiteSpace(localImage) && File.Exists(localImage))
                    cachedImage.Source = ImageSource.FromFile(localImage);
                else
                    cachedImage.Source = null;
            }
            else if (cachedImage != null)
            {
                cachedImage.Source = null;
            }

            control.OnPropertyChanged(nameof(NaoTemImagemSelecionada));
            control.OnPropertyChanged(nameof(LoteFormImagem));
        }

        #region Bindable Properties (Apenas a estrutura para referência, seu código está OK)

        public static readonly BindableProperty NumeroFotoProperty = BindableProperty.Create(
            nameof(NumeroFoto), typeof(int), typeof(ImagemSelecao), default(int), BindingMode.TwoWay,
            propertyChanged: OnNumeroFotoChanged);

        private static void OnNumeroFotoChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ImagemSelecao)bindable;
            control.OnPropertyChanged(nameof(NumeroFoto));
        }

        public int NumeroFoto
        {
            get => (int)GetValue(NumeroFotoProperty);
            set => SetValue(NumeroFotoProperty, value);
        }

        public static readonly BindableProperty LoteFormImagemProperty = BindableProperty.Create(
            nameof(LoteFormImagem), typeof(LoteFormImagem), typeof(ImagemSelecao), default(LoteFormImagem), BindingMode.TwoWay,
            propertyChanged: ImageChanged);

        public LoteFormImagem LoteFormImagem
        {
            get => (LoteFormImagem)GetValue(LoteFormImagemProperty);
            set => SetValue(LoteFormImagemProperty, value);
        }

        public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
            nameof(IsReadOnly), typeof(bool), typeof(ImagemSelecao), false, BindingMode.TwoWay,
            propertyChanged: ReadOnlyChanged);

        private static void ReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((ImagemSelecao)bindable).OnPropertyChanged(nameof(IsReadOnly));
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion

        // Construtor padrão necessário para instância via XAML
        public ImagemSelecao()
        {
            InitializeComponent();
        }

        [RelayCommand]
        private async Task AddPhotoAsync()
        {
            try
            {
                if (IsReadOnly)
                {
                    Debug.WriteLine("[ImagemSelecao] IsReadOnly=true, ignorando toque.");
                    return;
                }

                Debug.WriteLine("[ImagemSelecao] AddPhotoAsync iniciado.");

                // Mostra diálogo para escolher entre câmera e galeria
                var useCamera = await PhotoPickerService.ShowPhotoSourceDialogAsync();
                
                if (useCamera == null)
                {
                    Debug.WriteLine("[ImagemSelecao] Usuário cancelou a seleção.");
                    return;
                }

                if (useCamera.Value)
                {
                    // Usar câmera
                    await OpenCameraAsync();
                }
                else
                {
                    // Usar galeria (Android Photo Picker - sem permissões necessárias)
                    await OpenGalleryAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImagemSelecao] EXCEÇÃO: {ex.Message}\n{ex.StackTrace}");
                await PopUpOK.ShowAsync("Erro", $"Falha ao adicionar foto: {ex.Message}");
            }
        }

        private async Task OpenCameraAsync()
        {
            // Verifica permissão de câmera
            PermissionStatus cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            Debug.WriteLine($"[ImagemSelecao] Permissão câmera atual: {cameraStatus}");

            if (cameraStatus != PermissionStatus.Granted)
            {
                cameraStatus = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.Camera>);
                Debug.WriteLine($"[ImagemSelecao] Permissão câmera após solicitação: {cameraStatus}");
            }

            if (cameraStatus != PermissionStatus.Granted)
            {
                await PopUpOK.ShowAsync("Permissão necessária", "Permita o acesso à câmera nas configurações do dispositivo para adicionar fotos.");
                return;
            }

            // Resolve o ViewModel
            var vm = ResolveViewModel();
            Debug.WriteLine($"[ImagemSelecao] ViewModel resolvido: {vm?.GetType().Name ?? "NULL"}");

            if (vm is null)
            {
                await PopUpOK.ShowAsync("Erro interno", "Não foi possível encontrar o contexto do formulário. Feche e abra a tela novamente.");
                Debug.WriteLine("[ImagemSelecao] ERRO: LoteFormularioViewModel não encontrado na árvore visual.");
                return;
            }

            Debug.WriteLine($"[ImagemSelecao] Abrindo câmera para foto {NumeroFoto}...");
            var cameraViewPage = new CameraViewPage(this, vm);
            await NavigationUtils.ShowPageAsModalAsync(cameraViewPage);
        }

        private async Task OpenGalleryAsync()
        {
            try
            {
                Debug.WriteLine("[ImagemSelecao] Abrindo galeria com Android Photo Picker...");

                // Android Photo Picker não requer permissões READ_MEDIA_IMAGES
                var photoResult = await PhotoPickerService.PickPhotoAsync();

                if (photoResult == null)
                {
                    Debug.WriteLine("[ImagemSelecao] Nenhuma foto selecionada da galeria.");
                    return;
                }

                // Processa a foto selecionada da galeria
                await ProcessGalleryPhoto(photoResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImagemSelecao] Erro ao abrir galeria: {ex.Message}");
                await PopUpOK.ShowAsync("Erro", $"Falha ao acessar galeria: {ex.Message}");
            }
        }

        private async Task ProcessGalleryPhoto(FileResult photoResult)
        {
            try
            {
                // Carrega a foto para o AppDataDirectory
                string? localImagePath = await LoadPhotoAsync(photoResult);

                if (string.IsNullOrEmpty(localImagePath))
                {
                    await PopUpOK.ShowAsync("Erro", "Falha ao carregar a foto selecionada.");
                    return;
                }

                // Resolve o ViewModel
                var vm = ResolveViewModel();
                if (vm == null)
                {
                    await PopUpOK.ShowAsync("Erro interno", "Não foi possível encontrar o contexto do formulário.");
                    return;
                }

                // Cria registro LoteFormImagem
                var localFilename = Path.GetFileName(localImagePath);

                LoteFormImagem = new LoteFormImagem
                {
                    LoteFormId = vm.LoteFormId,
                    url = localFilename
                };

                Debug.WriteLine($"[ImagemSelecao] LoteFormImagem criado da galeria: {localFilename}");

                // Atualiza UI
                AtualizaImagem(localImagePath);
                AtualizaUI();

                // Vincula ao ViewModel (Foto 1, 2 ou 3)
                switch (NumeroFoto)
                {
                    case 1:
                        vm.LoteFormImagem1 = LoteFormImagem;
                        Debug.WriteLine("[ImagemSelecao] Foto 1 atualizada da galeria");
                        break;
                    case 2:
                        vm.LoteFormImagem2 = LoteFormImagem;
                        Debug.WriteLine("[ImagemSelecao] Foto 2 atualizada da galeria");
                        break;
                    case 3:
                        vm.LoteFormImagem3 = LoteFormImagem;
                        Debug.WriteLine("[ImagemSelecao] Foto 3 atualizada da galeria");
                        break;
                }

                await PopUpOK.ShowAsync("Sucesso", "Foto adicionada com sucesso!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImagemSelecao] Erro ao processar foto da galeria: {ex.Message}");
                await PopUpOK.ShowAsync("Erro", $"Falha ao processar foto: {ex.Message}");
            }
        }

        private LoteFormularioViewModel? ResolveViewModel()
        {
            // Tenta primeiro no próprio ContentView
            if (BindingContext is LoteFormularioViewModel selfVm)
                return selfVm;

            // Sobe na árvore visual para encontrar a Page e pegar seu BindingContext
            var page = FindParentPage();
            if (page?.BindingContext is LoteFormularioViewModel pageVm)
                return pageVm;

            return null;
        }

        private Page? FindParentPage()
        {
            Element? current = this;
            while (current != null)
            {
                if (current is Page page)
                    return page;
                current = current.Parent;
            }
            return null;
        }

        // O método LoadPhotoAsync está correto para o MAUI, pois usa FileSystem.AppDataDirectory e APIs DOTNET
        public async Task<string> LoadPhotoAsync(FileResult photo)
        {
            if (photo == null) return null;

            try
            {
                var newFile = Path.Combine(FileSystem.AppDataDirectory, $"{DateTime.Now:yyyyMMddHHmmssfff}.jpg");

                await using var stream = await photo.OpenReadAsync();
                await using var newStream = File.OpenWrite(newFile);
                await stream.CopyToAsync(newStream);

                return newFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar a foto: {ex.Message}");
                return null;
            }
        }

        public bool NaoTemImagemSelecionada => LoteFormImagem == null;

        public void AtualizaUI()
        {
            OnPropertyChanged(nameof(NaoTemImagemSelecionada));
            OnPropertyChanged(nameof(LoteFormImagem));
        }

        // MUDANÇA: A propriedade CachedImage agora aponta para o elemento do XAML (via FindByName)
        public Image? CachedImage => CachedImageElement;

        public void AtualizaImagem(string fullImagePathFilename)
        {
            // O ImageSource.FromFile é o padrão MAUI
            if (CachedImage != null)
            {
                CachedImage.Source = ImageSource.FromFile(fullImagePathFilename);
                OnPropertyChanged(nameof(CachedImage));
            }
        }
    }
}