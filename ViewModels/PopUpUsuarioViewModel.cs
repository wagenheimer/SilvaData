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
        private Func<Task>? _closePopupAction;
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

            // Escuta mudanças no LoggedUser para atualizar a UI automaticamente
            _webService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ISIWebService.LoggedUser))
                {
                    AtualizarDadosUsuario();
                }
            };

            AtualizarDadosUsuario();
        }

        private void AtualizarDadosUsuario()
        {
            LoggedUserName = _webService.LoggedUser?.nome ?? string.Empty;
            LoggedUserEmail = _webService.LoggedUser?.email ?? string.Empty;
        }

        public void SetCloseAction(Func<Task> closeAction)
        {
            _closePopupAction = closeAction;
        }

        [RelayCommand]
        private async Task MinhaContaAsync()
        {
            if (_isClosing) return; _isClosing = true; if (_closePopupAction != null) await _closePopupAction();
            await NavigationUtils.ShowViewAsModalAsync<MinhaConta>();
        }

#if DEBUG
        [RelayCommand]
        private async Task PermissoesAsync()
        {
            if (_isClosing) return; _isClosing = true; if (_closePopupAction != null) await _closePopupAction();
            var popup = new PermissoesPopup();
            await NavigationUtils.ShowPopupAsync(popup);
        }
#endif

        [RelayCommand]
        private async Task PrivacidadeAsync()
        {
            if (_isClosing) return; _isClosing = true; if (_closePopupAction != null) await _closePopupAction(); // Fecha o popup
            await _configViewModel.MostraPrivacidade();
        }

        /// <summary>
        /// Fecha o popup e pergunta se o usuário quer deslogar.
        /// </summary>
        [RelayCommand]
        private async Task LogOffAsync()
        {
            if (_isClosing) return; _isClosing = true; if (_closePopupAction != null) await _closePopupAction(); // Fecha o popup
            await _configViewModel.PerguntaLogOff();
        }
    }
}
