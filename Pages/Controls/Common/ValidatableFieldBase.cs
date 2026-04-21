using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Utilities;

namespace SilvaData.Controls
{
    public abstract class ValidatableFieldBase : ContentView, ICampoObrigatorio
    {
        private bool _isValidationActive;
        private bool _isMessengerRegistered;

        public static readonly BindableProperty HasErrorProperty =
            BindableProperty.Create(
                nameof(HasError),
                typeof(bool),
                typeof(ValidatableFieldBase),
                false);

        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            protected set => SetValue(HasErrorProperty, value);
        }

        protected bool IsValidationActive => _isValidationActive;

        protected bool IsAnyValidationActive => _isValidationActive || IsGlobalValidationActiveForThisControl();

        protected ValidatableFieldBase()
        {
            RegisterValidationMessages();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext == null)
            {
                OnContextCleared();
                ApplyValidationVisualState(false);
                return;
            }

            if (ISIUtils.IsValidationActiveGlobal)
            {
                _isValidationActive = true;
            }

            OnContextAttached();

            if (IsAnyValidationActive)
            {
                ScheduleValidationRefresh();
            }
            else
            {
                ApplyValidationVisualState(false);
            }
        }

        public bool PreenchidoCorretamente()
        {
            bool hasError = ComputeHasError();
            UpdateValidationState(hasError, forceVisualRefresh: true);
            return !hasError;
        }

        protected void ScheduleValidationRefresh()
        {
            Action refresh = () => PreenchidoCorretamente();

            if (Dispatcher != null)
            {
                Dispatcher.Dispatch(refresh);
                return;
            }

            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Dispatch(refresh);
                return;
            }

            refresh();
        }

        protected void UpdateValidationState(bool hasError, bool forceVisualRefresh = false)
        {
            bool stateChanged = HasError != hasError;
            HasError = hasError;

            if (stateChanged || forceVisualRefresh)
            {
                ApplyValidationVisualState(hasError);
            }
        }

        protected virtual void OnContextAttached()
        {
        }

        protected virtual void OnContextCleared()
        {
        }

        protected virtual void ApplyValidationVisualState(bool hasError)
        {
        }

        protected abstract bool ComputeHasError();

        protected Page? GetOwningPage()
        {
            Element? current = this;
            while (current != null)
            {
                if (current is Page page)
                {
                    return page;
                }

                current = current.Parent;
            }

            return null;
        }

        protected bool IsGlobalValidationActiveForThisControl()
        {
            return ISIUtils.IsValidationActiveGlobal
                && ISIUtils.ValidationTargetPage != null
                && ReferenceEquals(GetOwningPage(), ISIUtils.ValidationTargetPage);
        }

        private bool IsMessageForThisControl(Page? targetPage)
        {
            return targetPage == null || ReferenceEquals(GetOwningPage(), targetPage);
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler == null)
            {
                UnregisterValidationMessages();
                return;
            }

            RegisterValidationMessages();
        }

        private void RegisterValidationMessages()
        {
            if (_isMessengerRegistered)
            {
                return;
            }

            WeakReferenceMessenger.Default.Register<HighlightRequiredFieldsMessage>(this, static (recipient, _) =>
            {
                var control = (ValidatableFieldBase)recipient;
                if (!control.IsMessageForThisControl(_.TargetPage))
                {
                    return;
                }

                control._isValidationActive = true;
                control.ScheduleValidationRefresh();
            });

            WeakReferenceMessenger.Default.Register<ClearValidationErrorsMessage>(this, static (recipient, _) =>
            {
                var control = (ValidatableFieldBase)recipient;
                if (!control.IsMessageForThisControl(_.TargetPage))
                {
                    return;
                }

                control._isValidationActive = false;
                control.UpdateValidationState(false, forceVisualRefresh: true);
                control.ScheduleValidationRefresh();
            });

            _isMessengerRegistered = true;
        }

        private void UnregisterValidationMessages()
        {
            if (!_isMessengerRegistered)
            {
                return;
            }

            WeakReferenceMessenger.Default.Unregister<HighlightRequiredFieldsMessage>(this);
            WeakReferenceMessenger.Default.Unregister<ClearValidationErrorsMessage>(this);
            _isMessengerRegistered = false;
        }
    }
}