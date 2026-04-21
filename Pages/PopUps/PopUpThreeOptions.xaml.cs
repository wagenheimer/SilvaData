using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilvaData.Utilities;

namespace SilvaData.Pages.PopUps
{
    public partial class PopUpThreeOptions : Popup<ExitAction>
    {
        public PopUpThreeOptions(string titulo, string mensagem, string textoSalvar, string textoDescartar, string textoCancelar)
        {
            InitializeComponent();
            BindingContext = new PopUpThreeOptionsViewModel(this, titulo, mensagem, textoSalvar, textoDescartar, textoCancelar);
        }

        public static async Task<ExitAction> ShowAsync(string titulo, string mensagem, string textoSalvar, string textoDescartar, string textoCancelar)
        {
            var popup = new PopUpThreeOptions(titulo, mensagem, textoSalvar, textoDescartar, textoCancelar);
            var result = await NavigationUtils.ShowPopupAsync<ExitAction>(popup);
            return result;
        }
    }

    public partial class PopUpThreeOptionsViewModel : ObservableObject
    {
        private readonly PopUpThreeOptions _popup;

        public string Titulo { get; }
        public string Mensagem { get; }
        public string TextoSalvar { get; }
        public string TextoDescartar { get; }
        public string TextoCancelar { get; }

        public PopUpThreeOptionsViewModel(PopUpThreeOptions popup, string titulo, string mensagem, string textoSalvar, string textoDescartar, string textoCancelar)
        {
            _popup = popup;
            Titulo = titulo;
            Mensagem = mensagem;
            TextoSalvar = textoSalvar;
            TextoDescartar = textoDescartar;
            TextoCancelar = textoCancelar;
        }

        [RelayCommand]
        private Task SalvarAsync()
        {
            return _popup.CloseAsync(ExitAction.Save);
        }

        [RelayCommand]
        private Task DescartarAsync()
        {
            return _popup.CloseAsync(ExitAction.Discard);
        }

        [RelayCommand]
        private Task CancelarAsync()
        {
            return _popup.CloseAsync(ExitAction.Cancel);
        }
    }
}
