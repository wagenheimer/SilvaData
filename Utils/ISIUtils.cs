using SilvaData.Models;
using SilvaData.Pages.PopUps;

using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics; // Usado para Debug.WriteLine
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace SilvaData.Utilities
{
    /// <summary>
    /// Classe de utilidades gerais para a aplicação ISI.
    /// </summary>
    public static class ISIUtils
    {
        // Constantes para evitar "magic strings" e centralizar os nomes de arquivos e chaves de preferência.
        private const string LoginInfoFileName = "logininfo.txt";
        private const string DeviceIdFileName = "deviceid.txt";
        private const string UserPreferenceKey = "user";
        private const string DeviceIdPreferenceKey = "my_id";

        /// <summary>
        /// Cria um arquivo de backup completo (.zip) contendo o banco de dados, informações de login, ID do dispositivo e imagens.
        /// </summary>
        /// <returns>O caminho completo para o arquivo .zip gerado.</returns>
        /// <exception cref="Exception">Lançada se a criação do arquivo de backup falhar.</exception>
        /// <remarks>
        /// Este método fecha temporariamente a conexão com o banco de dados para garantir a integridade do arquivo.
        /// A conexão é reaberta no bloco 'finally', garantindo que o app continue funcionando mesmo se o backup falhar.
        /// </remarks>
        public static async Task<string> CreateFullBackupAsync()
        {
            // O uso de 'finally' garante que o banco de dados seja reaberto, mesmo que ocorra um erro.
            await Database.CloseDatabaseAsync();

            string loginInfoPath = string.Empty;
            string deviceIdPath = string.Empty;

            try
            {
                // Usa o diretório de dados do aplicativo, recomendado pelo .NET MAUI.
                string appDataDir = FileSystem.AppDataDirectory;

                // --- Salva informações das Preferences em arquivos temporários ---
                loginInfoPath = Path.Combine(appDataDir, LoginInfoFileName);
                string user = Preferences.Get(UserPreferenceKey, null);
                await File.WriteAllTextAsync(loginInfoPath, user ?? string.Empty);

                deviceIdPath = Path.Combine(appDataDir, DeviceIdFileName);
                string device = Preferences.Get(DeviceIdPreferenceKey, null);
                await File.WriteAllTextAsync(deviceIdPath, device ?? string.Empty);

                // --- Monta o nome do arquivo de backup ---
                string userNameSanitized = SanitizeFileName(ISIWebService.Instance.LoggedUser.nome);
                string zipFileName = $"ISIApp {DateTime.Now:dd-MM-yyyy-HH_mm} - {userNameSanitized}.zip";
                string destinationZipPath = Path.Combine(appDataDir, zipFileName);

                // --- Lista todos os arquivos a serem incluídos no backup ---
                var filesToBackup = new List<string> { Database.PathDB, loginInfoPath, deviceIdPath };
                filesToBackup.AddRange(await LoteFormImagem.ListaImagensParaBackup());

                // --- Cria o arquivo .zip ---
                bool zipSuccess = TryCreateZipArchive(filesToBackup.ToArray(), destinationZipPath);

                if (!zipSuccess)
                {
                    // Se o zip falhar, lança uma exceção para que o chamador saiba do problema.
                    throw new Exception("Falha ao criar o arquivo de backup compactado.");
                }

                return destinationZipPath;
            }
            finally
            {
                // --- Limpeza e reabertura ---
                // Reabre o banco de dados para que o aplicativo volte a funcionar normalmente.
                await Database.ReopenDatabaseAsync();

                // Remove os arquivos temporários criados para o backup.
                if (File.Exists(loginInfoPath)) File.Delete(loginInfoPath);
                if (File.Exists(deviceIdPath)) File.Delete(deviceIdPath);
            }
        }

        /// <summary>
        /// Compacta uma lista de arquivos em um único arquivo .zip.
        /// </summary>
        /// <param name="filesToZip">Um array com os caminhos completos dos arquivos a serem compactados.</param>
        /// <param name="destinationZipFullPath">O caminho completo onde o arquivo .zip será salvo.</param>
        /// <returns>Verdadeiro se o arquivo .zip foi criado com sucesso, falso caso contrário.</returns>
        public static bool TryCreateZipArchive(string[] filesToZip, string destinationZipFullPath)
        {
            try
            {
                // Garante que não haja um arquivo antigo com o mesmo nome.
                if (File.Exists(destinationZipFullPath))
                {
                    File.Delete(destinationZipFullPath);
                }

                using (ZipArchive zip = ZipFile.Open(destinationZipFullPath, ZipArchiveMode.Create))
                {
                    foreach (var filePath in filesToZip)
                    {
                        // Verifica se o arquivo de origem realmente existe antes de tentar adicioná-lo.
                        if (File.Exists(filePath))
                        {
                            // Adiciona o arquivo ao zip, usando apenas o nome do arquivo como entrada no zip.
                            zip.CreateEntryFromFile(filePath, Path.GetFileName(filePath), CompressionLevel.Optimal);
                        }
                        else
                        {
                            Debug.WriteLine($"[TryCreateZipArchive] Arquivo não encontrado e ignorado: {filePath}");
                        }
                    }
                }

                // Confirma que o arquivo foi fisicamente criado no disco.
                return File.Exists(destinationZipFullPath);
            }
            catch (Exception ex)
            {
                // Em caso de erro, loga a exceção e retorna falso.
                Debug.WriteLine($"[TryCreateZipArchive] Exceção ao criar o arquivo zip: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove caracteres inválidos de uma string para que ela possa ser usada como nome de arquivo.
        /// </summary>
        /// <param name="fileName">O nome do arquivo original.</param>
        /// <returns>Uma string segura para ser usada como nome de arquivo.</returns>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            // Substitui cada caractere inválido por um hífen.
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '-');
            }
            return fileName;
        }

        /// <summary>
        /// Obtém a página atualmente visível para exibir diálogos ou acessar a MainPage de forma segura.
        /// </summary>
        /// <returns>A página atualmente visível, ou null se não houver.</returns>
        public static Page GetCurrentPage()
        {
            var mainPage = Microsoft.Maui.Controls.Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (mainPage == null)
                return null;

            // Se for NavigationPage, pega a página atual
            if (mainPage is NavigationPage navPage)
                return navPage.CurrentPage;

            // Se for TabbedPage, pega a página selecionada
            if (mainPage is TabbedPage tabbedPage)
                return tabbedPage.CurrentPage ?? tabbedPage;

            // Se for FlyoutPage/Shell, tenta pegar a página apresentada
            if (mainPage is FlyoutPage flyoutPage)
                return flyoutPage.Detail;

            if (mainPage is Shell shell)
                return shell.CurrentPage;

            // Caso contrário, retorna a MainPage
            return mainPage;
        }

        public static Task ShowErrorAsync(string title, string message)
        {
            var page = GetCurrentPage();
            return page == null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, "OK");
        }

        public static async Task TryOpenUriAsync(string uri, string errorTitle = "Erro")
        {
            try
            {
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(errorTitle, ex.Message);
            }
        }


        // Tries to resolve a Color from Application.Current.Resources; falls back if unavailable.
        public static Color ResolveColor(string key, Color fallback)
        {
            try
            {
                var resources = Microsoft.Maui.Controls.Application.Current?.Resources;
                if (resources != null && resources.TryGetValue(key, out var value))
                {
                    return value switch
                    {
                        Color c => c,
                        SolidColorBrush b => b.Color,
                        _ => fallback
                    };
                }
            }
            catch
            {
                // ignore and fallback
            }
            return fallback;
        }

        /// <summary>
        /// Remove diacríticos de uma string, retornando a versão sem acentos.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string RemoveDiacritics(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return text;

                string normalizedText = text.Normalize(NormalizationForm.FormD);
                var stringBuilder = new StringBuilder();

                foreach (char ch in normalizedText)
                {
                    UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (category != UnicodeCategory.NonSpacingMark)
                        stringBuilder.Append(ch);
                }

                return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao remover diacríticos: {ex.Message}");
                return text;
            }
        }

        /// <summary>
        /// Pega uma cor dos recursos da aplicação, retornando uma cor padrão se a chave não existir.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static Color GetResourceColor(string key, Color fallback)
        {
            if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color c)
                return c;
            return fallback;
        }

        /// <summary>
        /// ObservableCollection com suporte a AddRange/ReplaceRange (10x+ mais rápido)
        /// </summary>
        public class ObservableRangeCollection<T> : ObservableCollection<T>
        {
            private bool _suppressNotification = false;

            public ObservableRangeCollection() : base() { }

            public ObservableRangeCollection(IEnumerable<T> collection) : base(collection) { }

            /// <summary>
            /// Adiciona múltiplos itens de uma vez (MUITO mais rápido que loop de Add)
            /// </summary>
            public void AddRange(IEnumerable<T> items)
            {
                if (items == null) throw new ArgumentNullException(nameof(items));

                _suppressNotification = true;

                foreach (var item in items)
                {
                    Items.Add(item);
                }

                _suppressNotification = false;

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            /// <summary>
            /// Substitui todos os itens de uma vez (MUITO mais rápido que Clear + AddRange)
            /// </summary>
            public void ReplaceRange(IEnumerable<T> items)
            {
                if (items == null) throw new ArgumentNullException(nameof(items));

                _suppressNotification = true;

                Items.Clear();

                foreach (var item in items)
                {
                    Items.Add(item);
                }

                _suppressNotification = false;

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                if (!_suppressNotification)
                {
                    base.OnCollectionChanged(e);
                }
            }

            protected override void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (!_suppressNotification)
                {
                    base.OnPropertyChanged(e);
                }
            }
        }

        public static bool IsValidationActiveGlobal { get; set; } = false;

        public static Page? ValidationTargetPage { get; set; }

    }

    public static class ErrorHandler
    {
        public static async Task ShowErrorAsync(string titulo, string mensagem)
        {
            if (MainThread.IsMainThread)
                await PopUpOK.ShowAsync(titulo, mensagem);
            else
                await MainThread.InvokeOnMainThreadAsync(() => PopUpOK.ShowAsync(titulo, mensagem));
        }

        public static async Task ShowErrorAsync(string titulo, Exception ex)
        {
            var mensagem = ex is OperationCanceledException ? "Operação Cancelada" : ex.Message;
            await ShowErrorAsync(titulo, mensagem);
        }
    }
}