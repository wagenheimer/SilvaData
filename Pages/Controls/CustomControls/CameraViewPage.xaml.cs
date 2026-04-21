using CommunityToolkit.Maui.Core;
using System;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.ViewModels;

using System.Diagnostics;

namespace SilvaData.Controls
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // CAMERVIEWPAGE - Página de captura de fotos para LoteForm
    // ═══════════════════════════════════════════════════════════════════════════════
    // Responsabilidades:
    // - Controlar câmera (preview, flash, troca frontal/traseira)
    // - Capturar foto e salvar no AppDataDirectory
    // - Vincular foto ao LoteForm através de ImagemSelecao
    // - Feedback visual (animações, PopUpOK)
    // 
    // Navegação:
    // - Aberta por: LoteFormularioView (ao adicionar foto)
    // - Fecha após: Captura bem-sucedida ou cancelamento
    // ═══════════════════════════════════════════════════════════════════════════════

    public partial class CameraViewPage : ContentPage
    {
        // ───────────────────────────────────────────────────────────────────────────
        // CAMPOS PRIVADOS
        // ───────────────────────────────────────────────────────────────────────────

        private readonly ImagemSelecao _imagemSelecao;
        private readonly LoteFormularioViewModel _loteFormViewModel;
        private bool _isProcessing = false; // Lock para evitar múltiplos cliques
        private bool _isDisappearing = false; // Lock para evitar comandos durante fechamento

        // ───────────────────────────────────────────────────────────────────────────
        // PROPRIEDADE: FlashMode (Bindable)
        // ───────────────────────────────────────────────────────────────────────────

        private CameraFlashMode _flashMode = CameraFlashMode.Off;

        /// <summary>
        /// Modo atual do flash da câmera (Off, On, Auto).
        /// Propriedade bindável usada no XAML.
        /// </summary>
        public CameraFlashMode FlashMode
        {
            get => _flashMode;
            set
            {
                if (_flashMode != value)
                {
                    _flashMode = value;
                    OnPropertyChanged();
                }
            }
        }

        // ───────────────────────────────────────────────────────────────────────────
        // CONSTRUTOR
        // ───────────────────────────────────────────────────────────────────────────

        public CameraViewPage(ImagemSelecao imagemSelecao, LoteFormularioViewModel loteFormViewModel)
        {
            InitializeComponent();
            BindingContext = this;

            _imagemSelecao = imagemSelecao;
            _loteFormViewModel = loteFormViewModel;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // EVENTOS DE BOTÃO
        // ═══════════════════════════════════════════════════════════════════════════

        // ───────────────────────────────────────────────────────────────────────────
        // FlashButton_Clicked - Alterna entre Off → On → Auto → Off
        // ───────────────────────────────────────────────────────────────────────────

        private void FlashButton_Clicked(object sender, EventArgs e)
        {
            if (_isProcessing) return;

            try
            {
                _isProcessing = true;

                // Ciclo de flash: Off → On → Auto → Off
                FlashMode = FlashMode switch
                {
                    CameraFlashMode.Off => CameraFlashMode.On,
                    CameraFlashMode.On => CameraFlashMode.Auto,
                    CameraFlashMode.Auto => CameraFlashMode.Off,
                    _ => CameraFlashMode.Off
                };

                // Nota: O ícone do flash agora é atualizado via binding no XAML
                Debug.WriteLine($"[CameraViewPage] 🔄 Flash mode alterado para: {FlashMode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CameraViewPage] ❌ Erro ao alternar flash: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        // ───────────────────────────────────────────────────────────────────────────
        // CaptureButton_Clicked - Dispara captura de foto
        // ───────────────────────────────────────────────────────────────────────────
        // ★★★ CORRIGIDO: Agora usa Command ao invés de CaptureImage() direto ★★★
        // O Command dispara o evento MediaCaptured automaticamente
        // ───────────────────────────────────────────────────────────────────────────


        private void CaptureButton_Clicked(object sender, EventArgs e)
        {
            _ = CaptureButton_ClickedAsync();
        }

        private async Task CaptureButton_ClickedAsync()
        {
            if (_isProcessing) return;

            try
            {
                _isProcessing = true;
                captureButton.IsEnabled = false;

                Debug.WriteLine("[CameraViewPage] 📸 Iniciando captura de foto...");

                // Animação visual de clique
                await AnimateCaptureButton();

                // ★★★ CORREÇÃO: Usa o método CaptureImage() direto ★★★
                var result = await cameraView.CaptureImage(CancellationToken.None);

                if (result != null)
                {
                    Debug.WriteLine("[CameraViewPage] ✅ Foto capturada com sucesso!");

                    // Processa a imagem capturada
                    await ProcessCapturedImage(result);
                }
                else
                {
                    Debug.WriteLine("[CameraViewPage] ⚠️ CaptureImage retornou null");
                    await PopUpOK.ShowAsync("Erro", "Câmera não conseguiu capturar a foto. Tente novamente.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CameraViewPage] ❌ Erro ao capturar foto: {ex.Message}");
                Debug.WriteLine($"[CameraViewPage] ❌ StackTrace: {ex.StackTrace}");
                await PopUpOK.ShowAsync("Erro", $"Falha ao capturar foto: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                captureButton.IsEnabled = true;
            }
        }

        // ───────────────────────────────────────────────────────────────────────────
        // ProcessCapturedImage - Processa a imagem capturada
        // ───────────────────────────────────────────────────────────────────────────

        private async Task ProcessCapturedImage(Stream imageStream)
        {
            try
            {
                if (imageStream == null)
                {
                    Debug.WriteLine("[CameraViewPage] ❌ Stream de imagem é null");
                    await PopUpOK.ShowAsync("Atenção", "Nenhuma foto foi capturada.");
                    return;
                }

                Debug.WriteLine($"[CameraViewPage] ✅ Stream recebido: {imageStream.Length} bytes");

                // 1. Salva a foto no AppDataDirectory
                string? fullImagePathFilename = await SavePhotoStreamAsync(imageStream);

                if (string.IsNullOrEmpty(fullImagePathFilename))
                {
                    await PopUpOK.ShowAsync("Erro", "Falha ao salvar a foto. Tente novamente.");
                    return;
                }

                // 2. Cria registro LoteFormImagem
                var localFilename = Path.GetFileName(fullImagePathFilename);

                _imagemSelecao.LoteFormImagem = new LoteFormImagem
                {
                    LoteFormId = _loteFormViewModel.LoteFormId,
                    url = localFilename
                };

                Debug.WriteLine($"[CameraViewPage] ✅ LoteFormImagem criado: {localFilename}");

                // 3. Atualiza ImagemSelecao (binding com UI)
                _imagemSelecao.AtualizaImagem(fullImagePathFilename);
                _imagemSelecao.AtualizaUI();

                // 4. Vincula ao ViewModel (Foto 1, 2 ou 3)
                switch (_imagemSelecao.NumeroFoto)
                {
                    case 1:
                        _loteFormViewModel.LoteFormImagem1 = _imagemSelecao.LoteFormImagem;
                        Debug.WriteLine("[CameraViewPage] ✅ Foto 1 atualizada");
                        break;
                    case 2:
                        _loteFormViewModel.LoteFormImagem2 = _imagemSelecao.LoteFormImagem;
                        Debug.WriteLine("[CameraViewPage] ✅ Foto 2 atualizada");
                        break;
                    case 3:
                        _loteFormViewModel.LoteFormImagem3 = _imagemSelecao.LoteFormImagem;
                        Debug.WriteLine("[CameraViewPage] ✅ Foto 3 atualizada");
                        break;
                }

                // 5. Feedback de sucesso
                await PopUpOK.ShowAsync("Sucesso", "Foto capturada com sucesso!");

                // 6. Retorna para a página anterior
                await Task.Delay(500); // Delay para mostrar popup
                await Navigation.PopModalAsync();

                Debug.WriteLine("[CameraViewPage] ✅ Processo completo, retornando à página anterior");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CameraViewPage] ❌ Erro ao processar foto: {ex.Message}");
                Debug.WriteLine($"[CameraViewPage] ❌ StackTrace: {ex.StackTrace}");
                await PopUpOK.ShowAsync("Erro", $"Falha ao processar a foto: {ex.Message}");
            }
            finally
            {
                // Libera recursos do stream
                imageStream?.Dispose();
            }
        }

        // ───────────────────────────────────────────────────────────────────────────
        // AnimateCaptureButton - Efeito de pulsação no botão de captura
        // ───────────────────────────────────────────────────────────────────────────

        private async Task AnimateCaptureButton()
        {
            // Dupla pulsação rápida (efeito de "click")
            await captureButton.ScaleToAsync(0.90, 50, Easing.CubicOut);
            await captureButton.ScaleToAsync(1.0, 50, Easing.CubicIn);
            await captureButton.ScaleToAsync(0.95, 50, Easing.CubicOut);
            await captureButton.ScaleToAsync(1.0, 50, Easing.CubicIn);
        }

        // ───────────────────────────────────────────────────────────────────────────
        // SwitchCameraButton_Clicked - Alterna entre câmera frontal/traseira
        // ───────────────────────────────────────────────────────────────────────────

        private void SwitchCameraButton_Clicked(object sender, EventArgs e)
        {
            _ = SwitchCameraButton_ClickedAsync();
        }

        private async Task SwitchCameraButton_ClickedAsync()
        {
            if (_isProcessing) return;

            try
            {
                _isProcessing = true;

                var cameras = await cameraView.GetAvailableCameras(CancellationToken.None);

                if (cameras.Count < 2)
                {
                    await PopUpOK.ShowAsync("Aviso", "Apenas uma câmera disponível neste dispositivo.");
                    return;
                }

                // Alterna para a próxima câmera disponível
                cameraView.SelectedCamera = cameraView.SelectedCamera == cameras.FirstOrDefault()
                    ? cameras.LastOrDefault()
                    : cameras.FirstOrDefault();

                Debug.WriteLine($"[CameraViewPage] 🔄 Câmera alternada: {cameraView.SelectedCamera?.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CameraViewPage] ❌ Erro ao trocar câmera: {ex.Message}");
                await PopUpOK.ShowAsync("Erro", "Não foi possível trocar de câmera.");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        // ───────────────────────────────────────────────────────────────────────────
        // BackButton_Clicked - Confirma cancelamento antes de sair
        // ───────────────────────────────────────────────────────────────────────────

        private void BackButton_Clicked(object sender, EventArgs e)
        {
            _ = BackButton_ClickedAsync();
        }

        private async Task BackButton_ClickedAsync()
        {
            if (_isProcessing) return;

            try
            {
                _isProcessing = true;

                // Confirmação antes de descartar
                bool confirmou = await DisplayAlertAsync(
                    Traducao.confirmacao,
                    "Deseja sair sem capturar foto?",
                    Traducao.Sim,
                    Traducao.Nao
                );

                if (!confirmou) return;

                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CameraViewPage] ❌ Erro ao fechar: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // PROCESSAMENTO DA FOTO CAPTURADA
        // ═══════════════════════════════════════════════════════════════════════════

      

        // ───────────────────────────────────────────────────────────────────────────
        // SavePhotoStreamAsync - Salva stream da foto em arquivo JPG
        // ───────────────────────────────────────────────────────────────────────────
        // Retorna: Caminho completo do arquivo salvo ou null em caso de erro
        // ───────────────────────────────────────────────────────────────────────────

        private async Task<string?> SavePhotoStreamAsync(Stream photoStream)
        {
            if (photoStream == null) return null;

            try
            {
                // Nome do arquivo: IMG_20240315_143022_a3f9b2.jpg
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string uniqueId = Guid.NewGuid().ToString("N")[..6]; // Primeiros 6 caracteres
                string fileName = $"IMG_{timestamp}_{uniqueId}.jpg";

                string fullPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                // Salva stream em arquivo
                using (var fileStream = File.OpenWrite(fullPath))
                {
                    await photoStream.CopyToAsync(fileStream);
                }

                Debug.WriteLine($"[CameraViewPage] ✅ Foto salva: {fileName}");
                Debug.WriteLine($"[CameraViewPage] 📂 Caminho: {fullPath}");

                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CameraViewPage] ❌ Erro ao salvar foto: {ex.Message}");
                Debug.WriteLine($"[CameraViewPage] ❌ StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // LIFECYCLE DA PÁGINA
        // ═══════════════════════════════════════════════════════════════════════════

        // ───────────────────────────────────────────────────────────────────────────
        // OnAppearing - Inicia preview da câmera
        // ───────────────────────────────────────────────────────────────────────────

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (!_isDisappearing && cameraView.StartCameraPreviewCommand?.CanExecute(null) == true)
                {
                    cameraView.StartCameraPreviewCommand.Execute(null);
                    Debug.WriteLine("[CameraViewPage] ✅ Preview iniciado");
                }
                else
                {
                    Debug.WriteLine("[CameraViewPage] ⚠️ StartCameraPreviewCommand não disponível");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CameraViewPage] ❌ Erro ao iniciar preview: {ex.Message}");
            }
        }

        // ───────────────────────────────────────────────────────────────────────────
        // OnDisappearing - Para preview e limpa recursos
        // ───────────────────────────────────────────────────────────────────────────

        protected override void OnDisappearing()
        {
            _isDisappearing = true;
            base.OnDisappearing();

            try
            {
                // Para preview da câmera
                // Nota: Em algumas versões do CommunityToolkit.Maui no iOS, StopCameraPreviewCommand pode lançar SpecifiedCastException
                if (cameraView.StopCameraPreviewCommand?.CanExecute(null) == true)
                {
                    cameraView.StopCameraPreviewCommand.Execute(null);
                    Debug.WriteLine("[CameraViewPage] ✅ Preview parado");
                }

                // Reseta flash
                FlashMode = CameraFlashMode.Off;
            }
            catch (InvalidCastException ex)
            {
                // Erro conhecido em versões específicas do Toolkit no iOS - silenciamos para não confundir o log
                Debug.WriteLine($"[CameraViewPage] ℹ️ StopCameraPreview cast error (iOS known issue): {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CameraViewPage] ❌ Erro ao parar preview: {ex.Message}");
            }
        }
    }
}
