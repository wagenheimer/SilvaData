using System.ComponentModel;
using System.Diagnostics;

namespace SilvaData.Controls
{
    /// <summary>
    /// Um controle de seletor de hora customizado que implementa a validação.
    /// </summary>
    public partial class ISITimePicker : ValidatableFieldBase
    {
        #region BindableProperties

        private bool _isUpdatingInternal;

        /// <summary>
        /// Define se este campo é obrigatório (para fins de validação).
        /// </summary>
        public static readonly BindableProperty IsObrigatorioProperty =
            BindableProperty.Create(nameof(IsObrigatorio), typeof(bool), typeof(ISITimePicker), false);

        public bool IsObrigatorio
        {
            get => (bool)GetValue(IsObrigatorioProperty);
            set => SetValue(IsObrigatorioProperty, value);
        }

        /// <summary>
        /// O TimeSpan (hora) selecionado.
        /// </summary>
        public static readonly BindableProperty HoraProperty =
            BindableProperty.Create(
                nameof(Hora),
                typeof(TimeSpan?),
                typeof(ISITimePicker),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnHoraChanged);

        public TimeSpan? Hora
        {
            get => (TimeSpan?)GetValue(HoraProperty);
            set => SetValue(HoraProperty, value);
        }

        /// <summary>
        /// O texto de dica (label) exibido acima do controle.
        /// </summary>
        public static readonly BindableProperty HintProperty =
            BindableProperty.Create(nameof(Hint), typeof(string), typeof(ISITimePicker), string.Empty);

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        /// <summary>
        /// Define se o controle está em modo de apenas leitura.
        /// </summary>
        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(ISITimePicker),
                false,
                propertyChanged: OnIsReadOnlyChanged);

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion

        public ISITimePicker()
        {
            try
            {
                InitializeComponent();
                SyncTimePickerState();

                horaPicker.PropertyChanged += OnHoraPickerPropertyChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] ISITimePicker: {ex.Message}");
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
        }

        protected override void OnContextCleared()
        {
            HasError = false;
        }

        private static void OnHoraChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not ISITimePicker control)
            {
                return;
            }

            if (control._isUpdatingInternal)
            {
                return;
            }

            control.SyncTimePickerState();
            control.OnPropertyChanged(nameof(PrecisaMostrarApagar));
            control.ScheduleValidationRefresh();
        }

        private void SyncTimePickerState()
        {
            if (horaPicker == null)
            {
                return;
            }

            try
            {
                _isUpdatingInternal = true;
                horaPicker.Time = Hora ?? TimeSpan.Zero;
            }
            finally
            {
                _isUpdatingInternal = false;
            }
        }

        private void OnHoraPickerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != TimePicker.TimeProperty.PropertyName || _isUpdatingInternal)
            {
                return;
            }

            try
            {
                _isUpdatingInternal = true;
                Hora = horaPicker.Time;
            }
            finally
            {
                _isUpdatingInternal = false;
            }

            OnPropertyChanged(nameof(PrecisaMostrarApagar));
            ScheduleValidationRefresh();
        }

        private void ClearHora(object sender, EventArgs e)
        {
            if (IsReadOnly)
            {
                return;
            }

            Hora = null;
        }

        private static void OnIsReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not ISITimePicker control)
            {
                return;
            }

            control.OnPropertyChanged(nameof(PrecisaMostrarApagar));
            control.ScheduleValidationRefresh();
        }

        public bool PrecisaMostrarApagar => !IsReadOnly && Hora.HasValue;

        /// <summary>
        /// Implementação do ICampoObrigatorio (Validação)
        /// </summary>
        protected override bool ComputeHasError()
        {
            bool campoVazio = !Hora.HasValue;
            return IsObrigatorio && campoVazio && IsAnyValidationActive && !IsReadOnly;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
        }
    }
}
