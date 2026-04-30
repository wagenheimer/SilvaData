using System.Diagnostics;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace SilvaData.Pages.PopUps
{
    public partial class HtmlErrorPopup : ContentPage
    {
        private readonly string _htmlContent;
        private bool _isClosing;
        private readonly string _methodName;
        private readonly DateTime _timestamp;

        public HtmlErrorPopup(string htmlContent, string methodName)
        {
            InitializeComponent();

            _htmlContent = htmlContent ?? string.Empty;
            _methodName = methodName ?? "Unknown";
            _timestamp = DateTime.Now;

            MethodLabel.Text = $"MÃ©todo: {_methodName}";
            TimestampLabel.Text = $"Data/Hora: {_timestamp:dd/MM/yyyy HH:mm:ss}";

            // Carrega o HTML no WebView
            HtmlWebView.Source = new HtmlWebViewSource
            {
                Html = _htmlContent
            };

            Debug.WriteLine($"[HtmlErrorPopup] Exibindo erro HTML do mÃ©todo: {_methodName}");
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            _ = OnCloseClickedAsync();
        }

        private async Task OnCloseClickedAsync() { if (_isClosing) return; _isClosing = true;
            try
            {
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HtmlErrorPopup] Erro ao fechar: {ex.Message}");
            }
        }

        private void OnShareClicked(object sender, EventArgs e)
        {
            _ = OnShareClickedAsync();
        }

        private async Task OnShareClickedAsync()
        {
            try
            {
                var fileName = $"server_error_{_methodName}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await File.WriteAllTextAsync(filePath, _htmlContent);

                await Share.RequestAsync(new ShareFileRequest(
                    $"Erro do Servidor - {_methodName}",
                    new ShareFile(filePath)
                ));

                Debug.WriteLine($"[HtmlErrorPopup] HTML compartilhado: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HtmlErrorPopup] Erro ao compartilhar: {ex.Message}");
                await DisplayAlertAsync("Erro", $"NÃ£o foi possÃ­vel compartilhar: {ex.Message}", "OK");
            }
        }

        private void OnCopyClicked(object sender, EventArgs e)
        {
            _ = OnCopyClickedAsync();
        }

        private async Task OnCopyClickedAsync()
        {
            try
            {
                // Prepara o conteÃºdo para copiar com metadados
                var contentToCopy = $@"=== ERRO HTML DO SERVIDOR ===
MÃ©todo: {_methodName}
Data/Hora: {_timestamp:dd/MM/yyyy HH:mm:ss}
Tamanho do HTML: {_htmlContent.Length:N0} caracteres

=== CONTEÃšDO HTML ===
{_htmlContent}

==========================";

#if ANDROID
                await Clipboard.Default.SetTextAsync(contentToCopy);
#elif IOS
                await Clipboard.Default.SetTextAsync(contentToCopy);
#else
                await Clipboard.SetTextAsync(contentToCopy);
#endif

                // Feedback visual
                await DisplayAlertAsync("Copiado!", "O conteÃºdo HTML foi copiado para a Ã¡rea de transferÃªncia.", "OK");
                
                Debug.WriteLine($"[HtmlErrorPopup] HTML copiado para clipboard ({_htmlContent.Length} caracteres)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HtmlErrorPopup] Erro ao copiar: {ex.Message}");
                await DisplayAlertAsync("Erro", $"NÃ£o foi possÃ­vel copiar: {ex.Message}", "OK");
            }
        }
    }
}

