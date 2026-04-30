using SilvaData.Models;
using SilvaData.Utils;
using SilvaData.Controls;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using Syncfusion.Maui.Core;
using Syncfusion.Maui.ListView;
using Syncfusion.Maui.ListView.Helpers;
using Syncfusion.Maui.Inputs; // Para SfTextInputLayout
using System.Diagnostics;
using Sentry; // Para SentrySdk
using Sentry.Protocol; // Para AttachmentType
using System.Globalization;
using System.Text;

namespace SilvaData.Utils
{
    /// <summary>
    /// Classe de utilitários estáticos para validação, navegação e logging.
    /// </summary>
    public static class Helpers
    {
        private sealed class ValidationIssue
        {
            public ValidationIssue(View field, string title)
            {
                Field = field;
                Title = title;
            }

            public View Field { get; }
            public string Title { get; }
        }

        /// <summary>
        /// Normaliza texto para busca: minúsculas, sem acentos e sem diferenças de categoria unicode.
        /// </summary>
        public static string NormalizeForSearch(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var lower = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(lower.Length);
            foreach (var ch in lower)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).Trim();
        }

        /// <summary>
        /// Rola o SfListView para o próximo item após o item atual.
        /// </summary>
        public async static Task VaiParaProximo(View isi, SfListView camposaPreencher)
        {
            if (isi is ISIMacroNota isiMacroNota && camposaPreencher.DataSource?.DisplayItems != null)
            {
                var currentIndex = camposaPreencher.DataSource.DisplayItems.IndexOf(isiMacroNota.ISIMacroParametro!);

                if (currentIndex >= 0 && currentIndex < camposaPreencher.DataSource.DisplayItems.Count - 1)
                {
                    var nextItem = camposaPreencher.DataSource.DisplayItems[currentIndex + 1];
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        camposaPreencher.ScrollTo(nextItem, ScrollToPosition.Start, true));
                }
            }
        }

        /// <summary>
        /// Rola o SfListView para um item específico baseado no nome e categoria do parâmetro.
        /// </summary>
        public static async Task ScrollToISIMacroNotaPorNome(SfListView sfListView, string nome, string categoria)
        {
            if (sfListView?.DataSource?.Items == null) return;

            foreach (var item in sfListView.DataSource.Items.Cast<object>())
            {
                if (item is ParametroComAlternativas parametroComAlternativas)
                {
                    if (parametroComAlternativas.nome == nome && parametroComAlternativas.Categoria == categoria)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => sfListView.ScrollTo(item, ScrollToPosition.Start, true)).ConfigureAwait(false);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Rola um ScrollView para um elemento específico.
        /// </summary>
        private static async Task ScrollToElementAsync(ScrollView scrollView, View element)
        {
            try
            {
                // Timeout de segurança: ScrollToAsync pode travar no iOS com animated=true
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var scrollTask = scrollView.ScrollToAsync(element, ScrollToPosition.Center, true);
                var completedTask = await Task.WhenAny(scrollTask, Task.Delay(2000, cts.Token));

                if (completedTask != scrollTask)
                {
                    Debug.WriteLine("[ScrollToElement] ⚠️ Timeout — scroll não completou em 2s");
                }
                else
                {
                    cts.Cancel(); // Cancela o delay
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScrollToElement] Erro ignorado: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica todos os campos marcados como obrigatórios e destaca os erros.
        /// Suporta busca recursiva em layouts aninhados.
        /// </summary>
        public static async Task<bool> CheckCamposObrigatorios(
            Layout? camposDinamicos,
            List<View> CamposRequeridos,
            ScrollView? scrollView,
            SfListView? sfListView = null)
        {
            var validationIssues = new List<ValidationIssue>();
            var processedFields = new HashSet<View>();

            // 1. Check nos Campos Dinâmicos (Recursivo)
            if (camposDinamicos != null)
            {
                FindInvalidFields(camposDinamicos, validationIssues, processedFields);
            }

            // 2. Check nos Campos Normais (Caminho tradicional)
            if (CamposRequeridos != null)
            {
                foreach (var campo in CamposRequeridos)
                {
                    if (campo is ICampoObrigatorio obrigatorio)
                    {
                        if (!obrigatorio.PreenchidoCorretamente())
                        {
                            AddValidationIssue(validationIssues, processedFields, campo, GetValidationFieldTitle(campo));
                            Debug.WriteLine($"[VALIDATION] Campo Requerido (ICampoObrigatorio) com erro: {campo.GetType().Name}");
                        }
                    }

                    if (campo is SfTextInputLayout campoSf && campoSf.Content is Entry entrySf)
                    {
                        if (string.IsNullOrEmpty(entrySf.Text))
                        {
                            AddValidationIssue(validationIssues, processedFields, campo, campoSf.Hint);
                            campoSf.HasError = true;
                            Debug.WriteLine($"[VALIDATION] SfTextInputLayout vazio: {campoSf.Hint}");
                        }
                        else
                        {
                            campoSf.HasError = false;
                        }
                    }
                }
            }

            if (validationIssues.Count > 0)
            {
                var fieldWithErrorToZoom = validationIssues[0].Field;
                var missingFieldTitles = validationIssues
                    .Select(issue => issue.Title)
                    .Where(title => !string.IsNullOrWhiteSpace(title))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                try
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }
                catch (Exception) { /* Feature not supported */ }

                if (scrollView != null)
                {
                    Debug.WriteLine($"[CheckCampos] Antes de ScrollToElement — Thread={Environment.CurrentManagedThreadId}");
                    await ScrollToElementAsync(scrollView, fieldWithErrorToZoom);
                    Debug.WriteLine($"[CheckCampos] Depois de ScrollToElement — Thread={Environment.CurrentManagedThreadId}");
                }

                Debug.WriteLine($"[CheckCampos] Antes de RequiredFieldsPopup — Thread={Environment.CurrentManagedThreadId}");
                await ShowRequiredFieldsPopupAsync(missingFieldTitles);
                Debug.WriteLine($"[CheckCampos] Depois de RequiredFieldsPopup — Thread={Environment.CurrentManagedThreadId}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Exibe o popup customizado com a lista de campos obrigatórios pendentes.
        /// </summary>
        public static async Task ShowRequiredFieldsPopupAsync(IEnumerable<string?> fieldTitles)
        {
            var titles = fieldTitles
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Select(title => title!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (titles.Count == 0)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, Traducao.PorFavorPreencherCamposObrigatórios);
                return;
            }

            await RequiredFieldsPopup.ShowAsync(titles);
        }

        /// <summary>
        /// Busca recursivamente os campos inválidos em uma hierarquia de views.
        /// </summary>
        private static void FindInvalidFields(IView view, List<ValidationIssue> validationIssues, HashSet<View> processedFields)
        {
            // Se o próprio controle é validável
            if (view is ICampoObrigatorio obrigatorio)
            {
                if (!obrigatorio.PreenchidoCorretamente())
                {
                    if (view is View mauiView)
                    {
                        AddValidationIssue(validationIssues, processedFields, mauiView, GetValidationFieldTitle(mauiView));
                    }
                }
            }

            if (view is SfTextInputLayout textInputLayout && textInputLayout.Content is Entry textEntry)
            {
                bool isEmpty = string.IsNullOrWhiteSpace(textEntry.Text);
                textInputLayout.HasError = isEmpty;

                if (isEmpty && view is View textInputView)
                {
                    AddValidationIssue(validationIssues, processedFields, textInputView, textInputLayout.Hint);
                }
            }

            // Se for um container, busca nos filhos
            if (view is Microsoft.Maui.ILayout layout)
            {
                foreach (var child in layout)
                {
                    FindInvalidFields(child, validationIssues, processedFields);
                }
            }
            else if (view is ContentView contentView && contentView.Content != null)
            {
                FindInvalidFields(contentView.Content, validationIssues, processedFields);
            }
        }

        private static void AddValidationIssue(List<ValidationIssue> validationIssues, HashSet<View> processedFields, View field, string? title)
        {
            if (!processedFields.Add(field))
            {
                return;
            }

            validationIssues.Add(new ValidationIssue(field, NormalizeValidationTitle(title)));
        }

        private static string GetValidationFieldTitle(object? source)
        {
            if (source == null)
            {
                return string.Empty;
            }

            if (source is SfTextInputLayout textInputLayout && !string.IsNullOrWhiteSpace(textInputLayout.Hint))
            {
                return NormalizeValidationTitle(textInputLayout.Hint);
            }

            foreach (var propertyName in new[] { "Title", "Hint", "Header", "Text", "nome", "Nome", "Descricao", "Description" })
            {
                var value = TryGetStringProperty(source, propertyName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return NormalizeValidationTitle(value);
                }
            }

            if (source is Element element)
            {
                var titleLabel = element.FindByName<Label>("labelTitle");
                if (!string.IsNullOrWhiteSpace(titleLabel?.Text))
                {
                    return NormalizeValidationTitle(titleLabel.Text);
                }

                var bindingContextTitle = GetBindingContextValidationTitle(element.BindingContext);
                if (!string.IsNullOrWhiteSpace(bindingContextTitle))
                {
                    return NormalizeValidationTitle(bindingContextTitle);
                }
            }

            var nestedTitle = GetBindingContextValidationTitle(source);
            if (!string.IsNullOrWhiteSpace(nestedTitle))
            {
                return NormalizeValidationTitle(nestedTitle);
            }

            return string.Empty;
        }

        private static string GetBindingContextValidationTitle(object? source)
        {
            if (source == null)
            {
                return string.Empty;
            }

            if (source is ParametroComAlternativas parametro && !string.IsNullOrWhiteSpace(parametro.nome))
            {
                return parametro.nome;
            }

            foreach (var propertyName in new[] { "nome", "Nome", "Title", "Hint", "Header", "Descricao", "Description" })
            {
                var value = TryGetStringProperty(source, propertyName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return NormalizeValidationTitle(value);
                }
            }

            foreach (var nestedPropertyName in new[] { "ParametroComAlternativas", "ISIMacroParametro", "Nota", "BindingContext", "SelectedItem", "SelectedPropriedade", "SelectedRegional", "SelectedUnidadeEpidemiologica" })
            {
                var nestedProperty = source.GetType().GetProperty(nestedPropertyName);
                var nestedValue = nestedProperty?.GetValue(source);

                if (nestedValue == null || ReferenceEquals(nestedValue, source))
                {
                    continue;
                }

                var nestedTitle = GetBindingContextValidationTitle(nestedValue);
                if (!string.IsNullOrWhiteSpace(nestedTitle))
                {
                    return nestedTitle;
                }
            }

            return string.Empty;
        }

        private static string? TryGetStringProperty(object source, string propertyName)
        {
            var propertyInfo = source.GetType().GetProperty(propertyName);
            if (propertyInfo?.PropertyType != typeof(string))
            {
                return null;
            }

            return propertyInfo.GetValue(source) as string;
        }

        private static string NormalizeValidationTitle(string? title)
        {
            return string.IsNullOrWhiteSpace(title)
                ? string.Empty
                : title.Replace("*", string.Empty).Trim();
        }

        /// <summary>
        /// Invalida a medida de uma View (relíquia do Xamarin.Forms).
        /// </summary>
        public static void InvalidateSize(this View view)
        {
            if (view == null) return;
            view.InvalidateMeasure();
        }

        /// <summary>
        /// Exibe um pop-up de confirmação e fecha o modal se o usuário confirmar.
        /// </summary>
        public static void MostraConfirmacaodePopModal()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _ = await PopModalSeConfirmar().ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Exibe um PopUp "Sim/Não" perguntando se o usuário deseja sair.
        /// Se sim, fecha a página modal atual.
        /// </summary>
        public static async Task<bool> PopModalSeConfirmar()
        {
            var result = await PopUpYesNo.ShowAsync(Traducao.confirmacao, Traducao.sairformulario).ConfigureAwait(false);

            if (!result) return false;

            await NavigationUtils.PopModalAsync().ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Adiciona informações do usuário logado a um dicionário.
        /// </summary>
        public static Dictionary<string, object?> AdicionaInformacoesUsuarioLogado(Dictionary<string, object?>? properties = null)
        {
            properties ??= new Dictionary<string, object?>();
            var loggedUser = ISIWebService.Instance.LoggedUser; // SentryHelper depende disso

            if (loggedUser == null) return properties;

            properties.Add("UserId", loggedUser.id ?? "N/A");
            properties.Add("UserName", loggedUser.nome ?? "N/A");
            properties.Add("UserEmail", loggedUser.email ?? "N/A");
            properties.Add("DeviceId", loggedUser.dispositivoId ?? "N/A");

            return properties;
        }

        /// <summary>
        /// Obtém recursivamente os elementos filhos de uma View.
        /// </summary>
        private static IEnumerable<View> GetChildElements(this View view, Point point)
        {
            if (view is Layout layout)
            {
                foreach (View child in layout.Children)
                {
                    yield return child;
                    foreach (View descendant in GetChildElements(child, point))
                    {
                        yield return descendant;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Classe estática de helper para encapsular a lógica de logging de erros no Sentry.
    /// </summary>
    public static class SentryHelper
    {
        /// <summary>
        /// Captura uma exceção e a enriquece com dados do usuário e do método.
        /// </summary>
        public static void CaptureExceptionWithUser(Exception exception, string? methodname = null, string? encryptedresult = null)
        {
            if (exception == null) return;

            // Globally ignore network aborts and timeout to prevent Sentry flood
            var exType = exception.GetType().Name;
            var innerExType = exception.InnerException?.GetType().Name;
            var msg = exception.ToString().ToLowerInvariant();

            if (exType == "SocketException" || innerExType == "SocketException" ||
                exType == "HttpRequestException" || innerExType == "HttpRequestException" ||
                exType == "WebException" || innerExType == "WebException" ||
                exType == "Java.Net.SocketException" || innerExType == "Java.Net.SocketException" ||
                exType == "Java.Net.UnknownHostException" || innerExType == "Java.Net.UnknownHostException" ||
                exType == "OperationCanceledException" || innerExType == "OperationCanceledException")
            {
                return; // Do not log expected network / timeout issues
            }

            if (msg.Contains("socket closed") ||
                msg.Contains("connection abort") ||
                msg.Contains("connection reset") ||
                msg.Contains("network is unreachable") ||
                msg.Contains("the network connection was lost") ||
                msg.Contains("interrompida ou está instável") ||
                msg.Contains("verifique sua internet") ||
                msg.Contains("tempo limite excedido") ||
                msg.Contains("connection failure") ||
                msg.Contains("no address associated with hostname") ||
                msg.Contains("unable to resolve host") ||
                msg.Contains("eai_nodata") ||
                msg.Contains("eai_noname") ||
                msg.Contains("software caused connection abort") ||
                msg.Contains("broken pipe") ||
                msg.Contains("host is down"))
            {
                return; // Do not log expected network / timeout issues
            }

            var properties = new Dictionary<string, object?>();

            if (!string.IsNullOrEmpty(methodname))
            {
                properties.Add("methodname", methodname);
            }

            if (!string.IsNullOrEmpty(encryptedresult))
            {
                properties.Add("result", encryptedresult);
            }

            // CORREÇÃO: Chama o método estático da classe Helpers
            properties = Helpers.AdicionaInformacoesUsuarioLogado(properties);

            using (SentrySdk.PushScope())
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("tipo-erro", "WebService");
                    scope.SetExtras(properties);
                });
                SentrySdk.CaptureException(exception);
            }
        }

        /// <summary>
        /// Faz o log de um erro de Sincronização, anexando o banco de dados.
        /// </summary>
        internal static async Task LogErrorAsync(string updateJson, string tabela, string errorDetail)
        {
            var properties = new Dictionary<string, object?>
            {
                { "Detalhe do Erro", errorDetail },
                { "Json Data", updateJson }
            };

            // CORREÇÃO: Chama o método estático da classe Helpers
            properties = Helpers.AdicionaInformacoesUsuarioLogado(properties);

            // Adiciona o nome do usuário se AdicionaInformacoesUsuarioLogado falhar
            if (!properties.ContainsKey("User") && ISIWebService.Instance.LoggedUser != null)
            {
                properties.Add("User", $"{ISIWebService.Instance.LoggedUser.nome} - {ISIWebService.Instance.LoggedUser.email}");
            }

            var bancoDados = await File.ReadAllBytesAsync(Database.PathDB).ConfigureAwait(false);

            using (SentrySdk.PushScope())
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("tipo-erro", "Sincronização");
                    scope.SetExtras(properties);
                    scope.AddAttachment(bancoDados, "ISIDatabase.db3", AttachmentType.Default);
                });
                SentrySdk.CaptureException(new Exception($"Erro de Sincronização - {tabela}"));
            }
        }

    }
}