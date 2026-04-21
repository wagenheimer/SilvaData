using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Pages.PopUps;
using SilvaData.ViewModels;
using SilvaData.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SilvaData.Controls
{
    /// <summary>
    /// Classe base híbrida para páginas de edição (View).
    /// CORRIGIDO: Registra mensagens em OnAppearing, não no construtor.
    /// </summary>
    public partial class ContentPageEdit : ContentPage, IValidatablePage, IDisposable
    {
        public List<View> RequiredInputFields { get; protected set; }
        protected BaseEditViewModel? ViewModel => BindingContext as BaseEditViewModel;
        private bool _isDisposed = false;
        private bool _isRegistered = false; // Flag para evitar duplicação

        public ContentPageEdit()
        {
            InitializeComponent();
            RequiredInputFields = new List<View>();
            // REMOVIDO: Registros de mensagem aqui
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                ResetValidationStateIfOwned();
                WeakReferenceMessenger.Default.UnregisterAll(this);
            }
            _isDisposed = true;
        }
        #endregion

        /// <summary>
        /// Valida os campos (chamado pelo ViewModel via mensagem).
        /// </summary>
        public async Task<bool> ValidateFormAsync()
        {
            var scrollView = this.FindByName<ScrollView>("scrollView");
            var camposLayout = this.FindByName<Layout>("camposaPreencher");

            return await Helpers.CheckCamposObrigatorios(
                camposLayout,
                RequiredInputFields,
                scrollView);
        }

            protected bool IsValidationTarget(Page? targetPage)
            {
                return targetPage == null || ReferenceEquals(targetPage, this);
            }

        /// <summary>
        /// Comando interceptador (usado pelo Shell.BackButtonBehavior).
        /// </summary>
        [RelayCommand]
        private async Task BackButtonInvoked()
        {
            if (ViewModel != null && ViewModel.BackNowCommand.CanExecute(null))
            {
                await ViewModel.BackNowCommand.ExecuteAsync(null);
            }
            else
            {
                await BackNowInternal();
            }
        }

        /// <summary>
        /// Manipulador para a 'ConfirmExitRequestMessage' com 3 opções (Salvar, Descartar, Cancelar).
        /// </summary>
        private async Task HandleConfirmExitRequestAsync()
        {
            var resultado = await PopUpThreeOptions.ShowAsync(
                ViewModel?.Title ?? Traducao.confirmacao,
                Traducao.DesejaSalvarAlteracoes,
                Traducao.Salvar,
                Traducao.Descartar,
                Traducao.Cancelar);

            switch (resultado)
            {
                case ExitAction.Save:
                    WeakReferenceMessenger.Default.Send(new SaveAndCloseMessage());
                    break;
                case ExitAction.Discard:
                    WeakReferenceMessenger.Default.Send(new DiscardAndCloseMessage());
                    break;
                case ExitAction.Cancel:
                    // Não faz nada, permanece na página
                    break;
            }
        }

        /// <summary>
        /// Método interno que realmente fecha a página.
        /// </summary>
        private async Task BackNowInternal()
        {
            ResetValidationStateIfOwned();
            await NavigationUtils.PopModalAsync();
        }

        private void ResetValidationStateIfOwned()
        {
            if (!ReferenceEquals(ISIUtils.ValidationTargetPage, this))
            {
                return;
            }

            ISIUtils.IsValidationActiveGlobal = false;
            ISIUtils.ValidationTargetPage = null;
            WeakReferenceMessenger.Default.Send(new ClearValidationErrorsMessage(this));
        }

        /// <summary>
        /// ADICIONADO: Registra mensagens quando a página aparece.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = OnAppearingInternalAsync();
        }

        private async Task OnAppearingInternalAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[ContentPageEdit] OnAppearing chamado. _isRegistered={_isRegistered}, _hasLoaded={ViewModel?.HasLoaded}, IsBusy={ViewModel?.IsBusy}");

            // Limpa erros visuais da visita anterior, MAS só se não houver validação ativa.
            // No Android, o PopUpOK (DialogFragment) dispara OnAppearing ao fechar —
            // enviar ClearValidationErrors nesse caso apagaria a borda vermelha do campo com erro.
            if (!ISIUtils.IsValidationActiveGlobal)
            {
                WeakReferenceMessenger.Default.Send(new ClearValidationErrorsMessage(this));
            }

            ViewModel?.SetValidationHost(this);

            // ADICIONADO: Desregistra primeiro (evita duplicação)
            if (_isRegistered)
            {
                WeakReferenceMessenger.Default.Unregister<ValidateFormRequestMessage>(this);
                WeakReferenceMessenger.Default.Unregister<ConfirmExitRequestMessage>(this);
                WeakReferenceMessenger.Default.Unregister<ClosePageRequestMessage>(this);
                WeakReferenceMessenger.Default.Unregister<SaveAndCloseMessage>(this);
                WeakReferenceMessenger.Default.Unregister<DiscardAndCloseMessage>(this);
            }

            // ADICIONADO: Registra as mensagens
            WeakReferenceMessenger.Default.Register<ValidateFormRequestMessage>(this, async (r, m) =>
            {
                if (!IsValidationTarget(m.TargetPage))
                {
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[ContentPageEdit] ValidateFormRequestMessage recebida — Thread={Environment.CurrentManagedThreadId}");
                bool isValid = await ValidateFormAsync();
                System.Diagnostics.Debug.WriteLine($"[ContentPageEdit] ValidateFormAsync resultado={isValid} — Thread={Environment.CurrentManagedThreadId}");
                WeakReferenceMessenger.Default.Send(new ValidationCompleteMessage(isValid, this));
                System.Diagnostics.Debug.WriteLine($"[ContentPageEdit] ValidationCompleteMessage enviada");
            });

            WeakReferenceMessenger.Default.Register<ConfirmExitRequestMessage>(this, async (r, m) =>
            {
                await HandleConfirmExitRequestAsync();
            });

            WeakReferenceMessenger.Default.Register<ClosePageRequestMessage>(this, async (r, m) =>
            {
                await BackNowInternal();
            });

            WeakReferenceMessenger.Default.Register<SaveAndCloseMessage>(this, async (r, m) =>
            {
                // Solicita que o ViewModel salve e feche
                if (ViewModel != null)
                {
                    await ViewModel.SaveAndReturn();
                }
            });

            WeakReferenceMessenger.Default.Register<DiscardAndCloseMessage>(this, async (r, m) =>
            {
                // Fecha a página sem salvar
                await BackNowInternal();
            });

            _isRegistered = true;

            // Carrega dados do ViewModel
            if (ViewModel != null)
            {
                await ViewModel.AppearingBaseTask();
            }
        }

        /// <summary>
        /// ADICIONADO: Desregistra mensagens quando a página desaparece.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            System.Diagnostics.Debug.WriteLine($"[ContentPageEdit] OnDisappearing chamado. _isRegistered={_isRegistered}");

            // ADICIONADO: Desregistra as mensagens
            if (_isRegistered)
            {
                WeakReferenceMessenger.Default.Unregister<ValidateFormRequestMessage>(this);
                WeakReferenceMessenger.Default.Unregister<ConfirmExitRequestMessage>(this);
                WeakReferenceMessenger.Default.Unregister<ClosePageRequestMessage>(this);
                WeakReferenceMessenger.Default.Unregister<SaveAndCloseMessage>(this);
                WeakReferenceMessenger.Default.Unregister<DiscardAndCloseMessage>(this);
                _isRegistered = false;
            }
        }

        /// <summary>
        /// Substitui o botão "Voltar" do hardware (Android).
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
            if (ViewModel != null && ViewModel.BackNowCommand.CanExecute(null))
            {
                ViewModel.BackNowCommand.Execute(null);
            }
            else
            {
                _ = BackNowInternal();
            }
            return true;
        }
    }
}
