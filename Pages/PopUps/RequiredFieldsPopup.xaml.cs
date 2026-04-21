using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;
using System.Globalization;

namespace SilvaData.Pages.PopUps
{
    public partial class RequiredFieldsPopup : Popup<bool>
    {
        public RequiredFieldsPopup(IReadOnlyList<string> fieldTitles)
        {
            InitializeComponent();
            BindingContext = new RequiredFieldsPopupViewModel(this, fieldTitles);
        }

        public static async Task ShowAsync(IEnumerable<string> fieldTitles)
        {
            var titles = fieldTitles
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Select(title => title.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (titles.Count == 0)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, Traducao.PorFavorPreencherCamposObrigatórios);
                return;
            }

            var popup = new RequiredFieldsPopup(titles);
            await NavigationUtils.ShowPopupAsync(popup);
        }
    }

    public partial class RequiredFieldsPopupViewModel : ObservableObject
    {
        private readonly RequiredFieldsPopup _popup;

        public string Titulo { get; }
        public string SubTitulo { get; }
        public string Descricao { get; }
        public string BotaoTexto { get; }
        public string QuantidadeCampos { get; }
        public string ResumoPendencias { get; }
        public ObservableCollection<string> Campos { get; }

        public RequiredFieldsPopupViewModel(RequiredFieldsPopup popup, IReadOnlyList<string> fieldTitles)
        {
            _popup = popup;
            int quantidadeCampos = fieldTitles.Count;

            Titulo = Traducao.PorFavorPreencherCamposObrigatórios;
            SubTitulo = quantidadeCampos == 1
                ? GetLocalizedString("UmCampoPendente", "1 campo pendente")
                : string.Format(
                    CultureInfo.CurrentUICulture,
                    GetLocalizedString("CamposPendentesQuantidade", "{0} campos pendentes"),
                    quantidadeCampos);
            Descricao = GetLocalizedString(
                "ReviseOsCamposAbaixoECompleteOQueFaltaParaContinuar",
                "Revise os campos abaixo e complete o que falta para continuar.");
            BotaoTexto = Traducao.OK;
            QuantidadeCampos = quantidadeCampos.ToString(CultureInfo.CurrentUICulture);
            ResumoPendencias = quantidadeCampos == 1
                ? Traducao.Pendente
                : GetLocalizedString("Pendentes", "Pendentes");
            Campos = new ObservableCollection<string>(fieldTitles);
        }

        private static string GetLocalizedString(string key, string fallback)
        {
            return Traducao.ResourceManager.GetString(key, Traducao.Culture) ?? fallback;
        }

        [RelayCommand]
        private Task OK()
        {
            return _popup.CloseAsync(true);
        }
    }
}
