using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Pages.PopUps;
using SilvaData.Utilities;

using System.ComponentModel;
using System.Diagnostics;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// Interface que define o contrato para ViewModels de edição.
    /// Permite binding genérico em controles e DataTemplates.
    /// </summary>
    public interface IEditableViewModel
    {
        bool ReadOnly { get; }
        bool IsBusy { get; }
        string Title { get; }
        string SubTitle { get; }
    }

    /// <summary>
    /// Classe base para ViewModels de edição.
    /// Implementa IEditableViewModel para facilitar bindings genéricos.
    /// </summary>
    public abstract partial class BaseEditViewModel : ViewModelBase, IEditableViewModel, IDisposable
    {
        protected bool _hasLoaded = false;

        public bool HasLoaded => _hasLoaded;

        private TaskCompletionSource<bool>? _validationTcs;
        private bool _isDisposed = false;
        private WeakReference<Page>? _validationHostPage;

        public bool NotReadOnly => !ReadOnly;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string subTitle = string.Empty;

        public bool NovoRegistro { get; set; } = false;
        public bool DataSaved { get; set; } = true;
        public virtual bool PodeEditar => true;

        public BaseEditViewModel()
        {
            WeakReferenceMessenger.Default.Register<ValidationCompleteMessage>(this, (r, m) =>
            {
                var validationPage = GetValidationHostPage();
                if (validationPage == null || m.SourcePage == null || !ReferenceEquals(validationPage, m.SourcePage))
                {
                    return;
                }

                _validationTcs?.TrySetResult(m.IsValid);
            });
        }

        public void SetValidationHost(Page page)
        {
            _validationHostPage = new WeakReference<Page>(page);
        }

        protected Page? GetValidationHostPage()
        {
            if (_validationHostPage != null && _validationHostPage.TryGetTarget(out var page))
            {
                return page;
            }

            return null;
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
                WeakReferenceMessenger.Default.UnregisterAll(this);
            }
            _isDisposed = true;
        }
        #endregion

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName != nameof(IsBusy) &&
                e.PropertyName != nameof(NotBusy) &&
                e.PropertyName != nameof(ReadOnly) &&
                e.PropertyName != nameof(NotReadOnly) &&
                e.PropertyName != nameof(Title) &&
                e.PropertyName != nameof(SubTitle))
            {
                DataSaved = false;
            }
        }

        /// <summary>
        /// ✅ REESCRITO: Garante que comandos sejam notificados na Thread Principal quando IsBusy mudar.
        /// </summary>
        protected override void OnIsBusyChanged()
        {
            System.Diagnostics.Debug.WriteLine($"[BaseEdit] OnIsBusyChanged — IsBusy={IsBusy}, NotBusy={NotBusy}, Thread={Environment.CurrentManagedThreadId}");
            base.OnIsBusyChanged();

            EditCommand.NotifyCanExecuteChanged();
            SaveAndReturnCommand.NotifyCanExecuteChanged();
            SaveAddNewCommand.NotifyCanExecuteChanged();
            CancelAddNewCommand.NotifyCanExecuteChanged();
            BackNowCommand.NotifyCanExecuteChanged();
            CheckSavedCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Comando para editar (desativa modo ReadOnly).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEdit))]
        public virtual void Edit()
        {
            IsReadOnly = false;
        }

        public bool CanEdit() => PodeEditar && NotBusy && ReadOnly;

        /// <summary>
        /// Comando para salvar e voltar (abstrato, implementado nas subclasses).
        /// </summary>
        [RelayCommand(CanExecute = nameof(NotBusy), AllowConcurrentExecutions = false)]
        public abstract Task SaveAndReturn();

        /// <summary>
        /// Comando para salvar um novo registro.
        /// </summary>
        [RelayCommand(CanExecute = nameof(NotBusy), AllowConcurrentExecutions = false)]
        public virtual Task SaveAddNew() => SaveAndReturn();

        /// <summary>
        /// Comando para cancelar a adição de um novo registro.
        /// </summary>
        [RelayCommand(CanExecute = nameof(NotBusy), AllowConcurrentExecutions = false)]
        public virtual Task CancelAddNew()
        {
            return BackNow();
        }

        /// <summary>
        /// Comando para voltar sem salvar (com confirmação se houver mudanças).
        /// </summary>
        [RelayCommand(CanExecute = nameof(NotBusy), AllowConcurrentExecutions = false)]
        public virtual Task BackNow()
        {
            if (!DataSaved && !ReadOnly)
            {
                WeakReferenceMessenger.Default.Send(new ConfirmExitRequestMessage());
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new ClosePageRequestMessage());
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Comando para verificar se há mudanças e voltar (ou salvar).
        /// Diferencia entre modo edição (ReadOnly=false) e visualização (ReadOnly=true).
        /// </summary>
        [RelayCommand(CanExecute = nameof(NotBusy), AllowConcurrentExecutions = false)]
        public virtual async Task CheckSaved()
        {
            Debug.WriteLine($"[BaseEditViewModel] CheckSaved: ReadOnly={ReadOnly}, DataSaved={DataSaved}");

            // Se está em modo visualização (ReadOnly), apenas volta
            if (ReadOnly)
            {
                await NavigationUtils.PopModalAsync();
                return;
            }

            // Se está em modo edição e há mudanças não salvas
            if (!DataSaved)
            {
                var resultado = await PopUpThreeOptions.ShowAsync(
                    Traducao.confirmacao,
                    Traducao.DesejaSalvarAlteracoes,
                    Traducao.Salvar,
                    Traducao.Descartar,
                    Traducao.Cancelar);

                switch (resultado)
                {
                    case ExitAction.Save:
                        // Tenta salvar
                        await SaveAndReturn();
                        break;
                    case ExitAction.Discard:
                        // Fecha sem salvar
                        await NavigationUtils.PopModalAsync();
                        break;
                    case ExitAction.Cancel:
                        // Fica na página (não volta)
                        break;
                }
            }
            else
            {
                // Sem mudanças, apenas volta
                await NavigationUtils.PopModalAsync();
            }
        }

        /// <summary>
        /// Método abstrato para carregar/criar itens.
        /// </summary>
        public abstract Task GetItemOrCreateANew();

        /// <summary>
        /// Método virtual para criar campos adicionais (override em subclasses).
        /// </summary>
        public virtual Task CreateAdditionalFields() => Task.CompletedTask;

        /// <summary>
        /// Método chamado quando a página aparece (Appearing).
        /// Virtual para permitir override em subclasses.
        /// </summary>
        public virtual async Task AppearingBaseTask()
        {
            if (_hasLoaded || IsBusy) return;
            DataSaved = false;
            IsBusy = true;
            ISIUtils.IsValidationActiveGlobal = false;
            ISIUtils.ValidationTargetPage = null;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                await GetItemOrCreateANew();
                await CreateAdditionalFields();
                _hasLoaded = true;
                DataSaved = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Envia uma mensagem à View solicitando validação e aguarda o resultado.
        /// </summary>
        protected async Task<bool> ValidateViewAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[BaseEdit.ValidateViewAsync] INÍCIO — Thread={Environment.CurrentManagedThreadId}");
            _validationTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var validationPage = GetValidationHostPage();

            ISIUtils.IsValidationActiveGlobal = true;
            ISIUtils.ValidationTargetPage = validationPage;

            // ✅ Solicita aos controles customizados (ICampoObrigatorio) que mostrem destaque visual (ex: borda vermelha)
            WeakReferenceMessenger.Default.Send(new HighlightRequiredFieldsMessage(validationPage));

            WeakReferenceMessenger.Default.Send(new ValidateFormRequestMessage(validationPage));
            System.Diagnostics.Debug.WriteLine($"[BaseEdit.ValidateViewAsync] Aguardando _validationTcs...");

            // Timeout de segurança: se a validação travar (ex: ScrollToAsync no iOS), não bloqueia para sempre
            var completedTask = await Task.WhenAny(_validationTcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            bool isValid;
            if (completedTask == _validationTcs.Task)
            {
                isValid = _validationTcs.Task.Result;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BaseEdit.ValidateViewAsync] ⚠️ TIMEOUT — validação não completou em 15s, assumindo inválido");
                isValid = false;
            }

            System.Diagnostics.Debug.WriteLine($"[BaseEdit.ValidateViewAsync] Resultado={isValid} — Thread={Environment.CurrentManagedThreadId}");
            _validationTcs = null;
            return isValid;
        }
    }
}
