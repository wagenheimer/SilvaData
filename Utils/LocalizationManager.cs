using System.Globalization;
using System.Text;

using CommunityToolkit.Mvvm.ComponentModel;

using LocalizationResourceManager.Maui;

using Microsoft.Maui.Controls;

namespace SilvaData.Utils
{
    public class LocalizationManager : ObservableObject
    {
        private static LocalizationManager locManager = null;

        public string CurrentLanguage { get; set; }

        public static LocalizationManager LocManager
        {
            get
            {
                if (locManager == null) Start();
                return locManager;
            }
        }

        public string IdiomaParaWebService => CurrentLanguage == "pt-BR" ? "BR" : CurrentLanguage == "es-ES" ? "SP" : "EN";
        public string IdiomaParaCalendario => CurrentLanguage == "pt-BR" ? "pt" : CurrentLanguage == "es-ES" ? "es" : "en";


        public LocalizationManager()
        {
            var selectedlanguage = Preferences.Get("newlanguage", "pt-BR");

            SetLanguage(selectedlanguage);
        }

        internal static void Start()
        {
            locManager ??= new LocalizationManager();
        }

        /// <summary>
        /// Remove Todos os Caracteres Especiais
        /// </summary>
        public static string RemoveDiacritics(string text)
        {
            var formD = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Altera a Linguagem Atual
        /// </summary>
        internal void SetLanguage(string linguagem)
        {
            CurrentLanguage = linguagem;

            Preferences.Set("newlanguage", linguagem);

            // Obter o serviço de localização e definir a cultura
            var localizationResourceManager = Application.Current?.Handler?.MauiContext?.Services?.GetService<ILocalizationResourceManager>();
            if (localizationResourceManager != null)
            {
                localizationResourceManager.CurrentCulture = new CultureInfo(linguagem);
            }

            // Aplica na thread atual e em todas as novas threads criadas pelo runtime
            var culture = new CultureInfo(linguagem);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Notificar mudanças nas propriedades
            OnPropertyChanged(nameof(CurrentLanguage));
            OnPropertyChanged(nameof(IdiomaParaWebService));
            OnPropertyChanged(nameof(IdiomaParaCalendario));
            SilvaData.Extensions.SafeLocalizationSource.Instance.NotifyAllChanged();
        }
    }
}