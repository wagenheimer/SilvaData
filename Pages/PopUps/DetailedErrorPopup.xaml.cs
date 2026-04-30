using SilvaData.Models;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.Diagnostics;
using System.Text;

namespace SilvaData.Pages.PopUps
{
    public partial class DetailedErrorPopup : ContentPage
    {
        private readonly ErrorDetails _errorDetails;
        private bool _isClosing;

        public DetailedErrorPopup(ErrorDetails errorDetails)
        {
            InitializeComponent();

            _errorDetails = errorDetails ?? throw new ArgumentNullException(nameof(errorDetails));

            InitializeUI();
            PopulateErrorDetails();
        }

        private void InitializeUI()
        {
            // Configura o tipo de erro
            ErrorTypeLabel.Text = _errorDetails.ErrorType.ToString().ToUpper();
            
            // Define a cor do frame baseado no tipo de erro
            var frameColor = _errorDetails.ErrorType switch
            {
                ErrorType.NetworkError => Colors.Orange,
                ErrorType.TimeoutError => Colors.Red,
                ErrorType.ServerError => Colors.DarkRed,
                ErrorType.AuthenticationError => Colors.Purple,
                ErrorType.SerializationError => Colors.Brown,
                ErrorType.DecryptionError => Colors.Gray,
                ErrorType.HtmlError => Colors.Blue,
                ErrorType.CircuitBreakerOpen => Colors.DarkOrange,
                _ => Colors.Gray
            };

            ErrorTypeFrame.BackgroundColor = frameColor;

            // Configura informações básicas
            MethodLabel.Text = $"Método: {_errorDetails.MethodName}";
            TimestampLabel.Text = $"Data/Hora: {_errorDetails.Timestamp:dd/MM/yyyy HH:mm:ss} UTC";
            UserMessageLabel.Text = _errorDetails.UserFriendlyMessage;

            // Configura retry frame se houver retry
            if (_errorDetails.RetryCount > 0)
            {
                RetryFrame.IsVisible = true;
                RetryCountLabel.Text = $"Tentativas: {_errorDetails.RetryCount}";
                RetryableLabel.Text = $"Pode retentar: {(_errorDetails.IsRetryable ? "Sim" : "Não")}";
            }

            // Configura contexto se houver
            if (!_errorDetails.Context.Any())
            {
                ContextStack.IsVisible = false;
            }
        }

        private void PopulateErrorDetails()
        {
            // Informações da requisição
            RequestIdLabel.Text = $"Request ID: {_errorDetails.RequestId}";
            UrlLabel.Text = $"URL: {_errorDetails.RequestUrl}";
            StatusCodeLabel.Text = $"Status Code: {_errorDetails.StatusCode?.ToString() ?? "N/A"}";
            ResponseTimeLabel.Text = $"Tempo Resposta: {_errorDetails.ResponseTime?.TotalMilliseconds:F0}ms";

            // Payload
            var payloadText = string.IsNullOrEmpty(_errorDetails.RequestPayloadDecrypted) 
                ? _errorDetails.RequestPayload 
                : $"[Criptografado]\n{_errorDetails.RequestPayload}\n\n[Descriptografado]\n{_errorDetails.RequestPayloadDecrypted}";
            
            PayloadEditor.Text = payloadText;

            // Resposta
            ResponseEditor.Text = _errorDetails.ResponseContent;

            // Stack Trace
            StackTraceEditor.Text = _errorDetails.StackTrace;

            // Contexto adicional
            if (_errorDetails.Context.Any())
            {
                var contextText = new StringBuilder();
                foreach (var kvp in _errorDetails.Context)
                {
                    contextText.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
                ContextEditor.Text = contextText.ToString();
            }
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            _ = OnCloseClickedAsync();
        }

        private async Task OnCloseClickedAsync() 
        { 
            if (_isClosing) return; 
            _isClosing = true;
            try
            {
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DetailedErrorPopup] Erro ao fechar: {ex.Message}");
            }
        }

        private void OnToggleDetailsClicked(object sender, EventArgs e)
        {
            try
            {
                var isVisible = DetailsStack.IsVisible;
                DetailsStack.IsVisible = !isVisible;
                
                // Atualiza o texto do botão
                ToggleDetailsButton.Text = isVisible ? "▶ Ver Detalhes Técnicos" : "▼ Ocultar Detalhes Técnicos";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DetailedErrorPopup] Erro ao toggle detalhes: {ex.Message}");
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
                var contentToCopy = _errorDetails.ToFullDetails();

                await Clipboard.Default.SetTextAsync(contentToCopy);

                // Feedback visual
                await App.Current.MainPage.DisplayAlert("Copiado!", "Os detalhes do erro foram copiados para a área de transferência.", "OK");
                
                Debug.WriteLine($"[DetailedErrorPopup] Detalhes copiados para clipboard ({contentToCopy.Length} caracteres)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DetailedErrorPopup] Erro ao copiar: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Erro", $"Não foi possível copiar: {ex.Message}", "OK");
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
                var fileName = $"detailed_error_{_errorDetails.MethodName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                var content = _errorDetails.ToFullDetails();
                await File.WriteAllTextAsync(filePath, content);

                await Share.RequestAsync(new ShareFileRequest(
                    $"Erro Detalhado - {_errorDetails.MethodName}",
                    new ShareFile(filePath)
                ));

                Debug.WriteLine($"[DetailedErrorPopup] Detalhes compartilhados: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DetailedErrorPopup] Erro ao compartilhar: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Erro", $"Não foi possível compartilhar: {ex.Message}", "OK");
            }
        }
    }
}
