using SilvaData.Models;
using System.Diagnostics;

namespace SilvaData.Controls
{
    public partial class CampoTexto : ValidatableFieldBase
    {
        private ParametroComAlternativas? _parametroComAlternativas;

        public ParametroComAlternativas? ParametroComAlternativas
        {
            get => _parametroComAlternativas;
            set
            {
                if (_parametroComAlternativas != value)
                {
                    _parametroComAlternativas = value;
                    OnPropertyChanged(nameof(ParametroComAlternativas));
                    OnParametroChanged();
                }
            }
        }

        #region IsReadOnly BindableProperty
        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly), typeof(bool), typeof(CampoTexto), false, BindingMode.TwoWay,
                propertyChanged: (b, o, n) =>
                {
                    var control = (CampoTexto)b;
                    control.OnPropertyChanged(nameof(IsReadOnly));
                    control.OnPropertyChanged(nameof(NotIsReadOnly));
                    Debug.WriteLine($"[CampoTexto] IsReadOnly={n}");
                    control.ScheduleValidationRefresh();
                });

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool NotIsReadOnly => !IsReadOnly;

        #endregion

        #region Texto BindableProperty
        public static readonly BindableProperty TextoProperty =
            BindableProperty.Create(
                nameof(Texto), typeof(string), typeof(CampoTexto), null, BindingMode.TwoWay,
                propertyChanged: OnTextoChanged);

        private static void OnTextoChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CampoTexto)bindable;
            string texto = (string)newValue;

            if (control.ParametroComAlternativas != null)
            {
                control.ParametroComAlternativas.ValorString = texto;
                Debug.WriteLine($"[CampoTexto] Texto alterado: {texto}");

                control.ScheduleValidationRefresh();
            }
        }

        public string Texto
        {
            get => (string)GetValue(TextoProperty);
            set => SetValue(TextoProperty, value);
        }
        #endregion

        public CampoTexto()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] CampoTexto: {ex.Message}");
                Exception? inner = ex.InnerException;
                while (inner != null)
                {
                    Debug.WriteLine($"[CRASH_INNER] {inner.Message}");
                    Debug.WriteLine($"[CRASH_TRACE] {inner.StackTrace}");
                    inner = inner.InnerException;
                }
                throw;
            }
        }

        protected override void OnContextAttached()
        {
            if (BindingContext is ParametroComAlternativas param)
            {
                ParametroComAlternativas = param;
                return;
            }

            OnContextCleared();
        }

        protected override void OnContextCleared()
        {
            ParametroComAlternativas = null;
            Texto = null;
            HasError = false;
            SyncInnerFieldErrorState(false);
        }

        private void OnParametroChanged()
        {
            if (ParametroComAlternativas == null)
            {
                Texto = null;
                HasError = false;
                SyncInnerFieldErrorState(false);
                return;
            }

            if (string.IsNullOrEmpty(ParametroComAlternativas.ValorString) &&
                !string.IsNullOrEmpty(ParametroComAlternativas.valorPadrao))
            {
                ParametroComAlternativas.ValorString = ParametroComAlternativas.valorPadrao;
            }

            Texto = ParametroComAlternativas.ValorString;
            Debug.WriteLine($"[CampoTexto] BindingContext: {ParametroComAlternativas.nome}");
        }

        protected override bool ComputeHasError()
        {
            return ParametroComAlternativas?.required == 1
                && string.IsNullOrWhiteSpace(Texto)
                && IsAnyValidationActive
                && NotIsReadOnly;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            SyncInnerFieldErrorState(hasError);
        }

        private void SyncInnerFieldErrorState(bool hasError)
        {
            if (isiTextField == null)
            {
                return;
            }

            if (isiTextField.HasError == hasError)
            {
                if (hasError)
                {
                    isiTextField.HasError = false;
                }

                isiTextField.HasError = hasError;
                return;
            }

            isiTextField.HasError = hasError;
        }
    }
}
