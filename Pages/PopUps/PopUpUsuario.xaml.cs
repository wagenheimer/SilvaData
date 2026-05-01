using CommunityToolkit.Maui.Views;

using SilvaData.Infrastructure; // Para ServiceHelper
using SilvaData.ViewModels;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// PopUp que exibe o menu do usuï¿½rio (Minha Conta, Privacidade, Sair).
    /// </summary>
    public partial class PopUpUsuario : Popup<string>
    {
        private readonly PopUpUsuarioViewModel ViewModel;

        public PopUpUsuario()
        {
            InitializeComponent();

            // Resolve o ViewModel e suas dependências (ConfigViewModel, ISIWebService)
            ViewModel = ServiceHelper.GetRequiredService<PopUpUsuarioViewModel>();

            // Injeta a Ação de Fechar no ViewModel
            ViewModel.SetCloseAction(async (result) => { try { await CloseAsync(result?.ToString()); } catch { } });

            BindingContext = ViewModel;
        }
    }
}

