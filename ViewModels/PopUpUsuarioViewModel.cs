using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ISIInstitute.Views;

using SilvaData.Controls;
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using SilvaData.Utils;

using System;
using System.Threading.Tasks;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para o PopUp do Menu do Usuário (Minha Conta, Privacidade, Sair).
    /// </summary>
    public partial class PopUpUsuarioViewModel : ObservableObject
    {
        private readonly ConfigViewModel _configViewModel;
        private readonly ISIWebService _webService;
        private Func<object?, Task>? _closePopupAction;
        private bool _isClosing;

        [ObservableProperty]
        private string loggedUserName;

        [ObservableProperty]
        private string loggedUserEmail;

#if DEBUG
        [ObservableProperty]
        private bool isDebug = true;
#else
        [ObservableProperty]
        private bool isDebug = false;
#endif

        public PopUpUsuarioViewModel(ConfigViewModel configViewModel, ISIWebService webService)
        {
            _configViewModel = configViewModel;
            _webService = webService;

            AtualizarDadosUsuario();
        }

        private void AtualizarDadosUsuario()
        {
            LoggedUserName = _webService.LoggedUser?.nome ?? string.Empty;
            LoggedUserEmail = _webService.LoggedUser?.email ?? string.Empty;
        }

        public void SetCloseAction(Func<object?, Task> closeAction)
        {
            _closePopupAction = closeAction;
        }

        [RelayCommand]
        private async Task MinhaContaAsync()
        {
            if (_isClosing) return; _isClosing = true;
            if (_closePopupAction != null) await _closePopupAction("MinhaConta");
        }

#if DEBUG
        [RelayCommand]
        private async Task PermissoesAsync()
        {
            if (_isClosing) return; _isClosing = true;
            if (_closePopupAction != null) await _closePopupAction("Permissoes");
        }
#endif

        [RelayCommand]
        private async Task PrivacidadeAsync()
        {
            if (_isClosing) return; _isClosing = true;
            if (_closePopupAction != null) await _closePopupAction("Privacidade");
        }

        /// <summary>
        /// Fecha o popup e pergunta se o usuário quer deslogar.
        /// </summary>
        [RelayCommand]
        private async Task LogOffAsync()
        {
            if (_isClosing) return; _isClosing = true;
            if (_closePopupAction != null) await _closePopupAction("LogOff");
        }
    }
}
