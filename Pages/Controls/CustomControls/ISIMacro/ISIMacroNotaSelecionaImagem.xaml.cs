using SilvaData.Models;
using SilvaData.ViewModels;

using System.Diagnostics;

namespace SilvaData.Controls
{

    public partial class ISIMacroNotaSelecionaImagem : ContentPage
    {
        private readonly ISIMacroNotaSelecionaImagemViewModel _viewModel;
        private readonly bool _isIos;
        private string? _nome;
        private ParametroComAlternativas? _parametro;

        /// <summary>
        /// ✅ Construtor que recebe nome e parametro do NavigationUtils
        /// </summary>
        public ISIMacroNotaSelecionaImagem(string nome, ParametroComAlternativas parametro)
        {
            _nome = nome;
            _parametro = parametro;
            _isIos = DeviceInfo.Platform == DevicePlatform.iOS;

            _viewModel = ServiceHelper.GetRequiredService<ISIMacroNotaSelecionaImagemViewModel>();

            // ✅ iOS/Android fix: configurar os dados ANTES do InitializeComponent/BindingContext
            // para que o SfImageEditor receba o Source correto já na primeira renderização.
            _viewModel.SetInitialState(nome, parametro);

            // ★ Diagnóstico: Verifica existência do arquivo
            var alt = _viewModel.Alternativa;
            var rawUrl = alt?.urlImagem;
            var normalized = ParametroAlternativasFromWebService.NormalizeImageFileName(rawUrl);
            var path = ParametroAlternativasFromWebService.BuildLocalImagePath(rawUrl);
            bool exists = !string.IsNullOrEmpty(path) && File.Exists(path);
            long size = exists ? new FileInfo(path).Length : 0;
            Debug.WriteLine($"[ISIMacroFoto] ★ ctor altId={alt?.id} raw='{rawUrl ?? ""}' normalized='{normalized}' path='{path ?? "NULL"}' exists={exists} size={size} platform={DeviceInfo.Platform}");

            InitializeComponent();

            sfImageEditor.IsVisible = true;
            iosFallbackImage.IsVisible = false;
            Debug.WriteLine(_isIos
                ? "[ISIMacroFoto] Renderer ativo: SfImageEditor (iOS - stream mode)"
                : "[ISIMacroFoto] Renderer ativo: SfImageEditor");

            BindingContext = _viewModel;

            // ✅ Fix: SfImageEditor não recarrega de forma confiável via binding MAUI quando
            // a Source muda (iOS e Android). Atualização explícita via code-behind garante
            // que o controle carregue e navegue corretamente entre alternativas.
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Debug.WriteLine($"[ISIMacroNotaSelecionaImagem] Construtor: {nome}");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = ReloadImageEditorAsync(isFirstAppearance: true);
        }

        private async Task ReloadImageEditorAsync(bool isFirstAppearance = false)
        {
            // iOS: OnAppearing dispara DURANTE a animação do PushModalAsync.
            // Aguarda a animação terminar antes de tentar renderizar no SfImageEditor.
            if (isFirstAppearance && DeviceInfo.Platform == DevicePlatform.iOS)
                await Task.Delay(500);

            var path = _viewModel.Alternativa?.urlImagemLocal;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                var rawUrl = _viewModel.Alternativa?.urlImagem;
                var normalized = ParametroAlternativasFromWebService.NormalizeImageFileName(rawUrl);
                var rebuiltPath = ParametroAlternativasFromWebService.BuildLocalImagePath(rawUrl);
                var rebuiltExists = !string.IsNullOrEmpty(rebuiltPath) && File.Exists(rebuiltPath);
                Debug.WriteLine($"[ISIMacroFoto] ⚠️ ReloadImage: pathBinding='{path ?? "NULL"}' raw='{rawUrl ?? ""}' normalized='{normalized}' rebuilt='{rebuiltPath}' rebuiltExists={rebuiltExists}");
                sfImageEditor.Source = null;
                iosFallbackImage.Source = null;

                return;
            }

            var fileInfo = new FileInfo(path);
            Debug.WriteLine($"[ISIMacroFoto] 🔄 ReloadImage: {path} | bytes={fileInfo.Length}");

            sfImageEditor.Source = null;
            await Task.Delay(50);

            // Syncfusion recomenda stream com nova instância para reprocessamentos no iOS.
            if (_isIos)
            {
                var bytes = await File.ReadAllBytesAsync(path);
                sfImageEditor.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                Debug.WriteLine($"[ISIMacroFoto] ℹ️ Source aplicada no SfImageEditor via stream (iOS) | bytes={bytes.Length}");
            }
            else
            {
                sfImageEditor.Source = ImageSource.FromFile(path);
            }

            await Task.Delay(220);

            var originalSize = sfImageEditor.OriginalImageSize;
            var renderedSize = sfImageEditor.ImageRenderedSize;
            var sfLoaded = originalSize.Width > 0 && originalSize.Height > 0;

            Debug.WriteLine($"[ISIMacroFoto] 📐 SfImageEditor sizes original=({originalSize.Width},{originalSize.Height}) rendered=({renderedSize.Width},{renderedSize.Height}) loaded={sfLoaded}");

            if (_isIos && !sfLoaded)
            {
                iosFallbackImage.Source = ImageSource.FromFile(path);
                iosFallbackImage.IsVisible = true;
                sfImageEditor.IsVisible = false;
                Debug.WriteLine($"[ISIMacroFoto] ⚠️ SfImageEditor não renderizou no iOS, fallback nativo ativado: {path}");
                return;
            }

            iosFallbackImage.IsVisible = false;
            sfImageEditor.IsVisible = true;
            Debug.WriteLine($"[ISIMacroFoto] ✅ ReloadImage concluído (SfImageEditor): {path}");
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ISIMacroNotaSelecionaImagemViewModel.AlternativaImageSource))
                return;

            MainThread.BeginInvokeOnMainThread(() => _ = ReloadImageEditorAsync());
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel?.Cleanup();
            Debug.WriteLine("[ISIMacroNotaSelecionaImagem] Cleanup executado");
        }

        protected override bool OnBackButtonPressed()
        {
            try
            {
                _ = NavigationUtils.PopModalAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ISIMacroNotaSelecionaImagem] Erro: {ex.Message}");
                return base.OnBackButtonPressed();
            }
        }
    }
}