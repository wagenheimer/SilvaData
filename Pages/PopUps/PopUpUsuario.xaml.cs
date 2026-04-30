using CommunityToolkit.Maui.Views;

using SilvaData.Infrastructure; // Para ServiceHelper
using SilvaData.ViewModels;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// PopUp que exibe o menu do usuï¿½rio (Minha Conta, Privacidade, Sair).
    /// </summary>
    public partial class PopUpUsuario : Popup
    {
        private readonly PopUpUsuarioViewModel ViewModel;

        public PopUpUsuario()
        {
            InitializeComponent();

            // Resolve o ViewModel e suas dependï¿½ncias (ConfigViewModel, ISIWebService)
            ViewModel = ServiceHelper.GetRequiredService<PopUpUsuarioViewModel>();

            // Injeta a Aï¿½ï¿½o de Fechar no ViewModel
            ViewModel.SetCloseAction(async () => { try { await CloseAsync(); } catch { } });

            BindingContext = ViewModel;
        }
    }
}

