using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;

namespace SilvaData.Controls
{
    public partial class ISIDatePicker : ValidatableFieldBase, IDisposable
    {
        private bool _isDisposed = false;

        // FLAG DE CONTROLE PARA EVITAR LOOP
        private bool _isUpdatingInternal = false;

        #region IsObrigatorio
        public static readonly BindableProperty IsObrigatorioProperty =
            BindableProperty.Create(nameof(IsObrigatorio), typeof(bool), typeof(ISIDatePicker), false);

        public bool IsObrigatorio
        {
            get => (bool)GetValue(IsObrigatorioProperty);
            set => SetValue(IsObrigatorioProperty, value);
        }
        #endregion

        #region BindableProperties
        public static readonly BindableProperty HintProperty =
            BindableProperty.Create(nameof(Hint), typeof(string), typeof(ISIDatePicker), string.Empty);

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        public static readonly BindableProperty DataProperty =
            BindableProperty.Create(
                nameof(Data),
                typeof(DateTime?),
                typeof(ISIDatePicker),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnDatePropertyChanged);

        public DateTime? Data
        {
            get => (DateTime?)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly BindableProperty MinimumDateProperty =
            BindableProperty.Create(nameof(MinimumDate), typeof(DateTime), typeof(ISIDatePicker), new DateTime(1900, 1, 1));
        public DateTime MinimumDate { get => (DateTime)GetValue(MinimumDateProperty); set => SetValue(MinimumDateProperty, value); }

        public static readonly BindableProperty MaximumDateProperty =
            BindableProperty.Create(nameof(MaximumDate), typeof(DateTime), typeof(ISIDatePicker), new DateTime(2100, 12, 31));
        public DateTime MaximumDate { get => (DateTime)GetValue(MaximumDateProperty); set => SetValue(MaximumDateProperty, value); }

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(ISIDatePicker), false);
        public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

        public event EventHandler<DateChangedEventArgs>? DateChanged;
        #endregion

        public ISIDatePicker()
        {
            InitializeComponent();

            SyncDatePickerState();

            dataPicker.DateSelected += OnInternalDatePickerSelected;

            WeakReferenceMessenger.Default.Register<MudouDataLoteMessage>(this, HandleMudouDataLoteMessage);
        }

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
                WeakReferenceMessenger.Default.Unregister<MudouDataLoteMessage>(this);
                dataPicker.DateSelected -= OnInternalDatePickerSelected;
            }
            _isDisposed = true;
        }

        private static void OnDatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not ISIDatePicker control) return;

            if (control._isUpdatingInternal) return;

            DateTime? newDate = (DateTime?)newValue;

            control.SyncDatePickerState();

            if (!newDate.HasValue)
            {
                control.ScheduleValidationRefresh();
                return;
            }

            DateTime targetDate = newDate.Value;

            if (control.dataPicker.Date != targetDate.Date)
            {
                control.dataPicker.Date = targetDate;
            }

            control.DateChanged?.Invoke(control, new DateChangedEventArgs(
                (DateTime?)oldValue ?? DateTime.MinValue,
                targetDate));

            control.ScheduleValidationRefresh();
        }

        private void OnInternalDatePickerSelected(object? sender, DateChangedEventArgs e)
        {
            if (Data == e.NewDate) return;

            try
            {
                _isUpdatingInternal = true;

                Data = e.NewDate;
                WeakReferenceMessenger.Default.Send(new MudouDataLoteMessage());

                DateChanged?.Invoke(this, e);

                ScheduleValidationRefresh();
            }
            finally
            {
                _isUpdatingInternal = false;
            }
        }

        private void OnCalendarButtonClicked(object sender, EventArgs e)
        {
            if (!IsReadOnly)
            {
                dataPicker.Focus();
            }
        }

        private void HandleMudouDataLoteMessage(object recipient, MudouDataLoteMessage message)
        {
            DateChanged?.Invoke(this, new DateChangedEventArgs(Data ?? DateTime.Now, Data ?? DateTime.Now));
        }

        private void SyncDatePickerState()
        {
            if (dataPicker == null)
            {
                return;
            }

            dataPicker.Date = Data ?? DateTime.Today;
        }

        protected override void OnContextAttached()
        {
        }

        protected override void OnContextCleared()
        {
            HasError = false;
            ErrorLabel.IsVisible = false;
        }

        protected override bool ComputeHasError()
        {
            bool campoVazio = !IsValid();
            return IsObrigatorio && campoVazio && IsAnyValidationActive && !IsReadOnly;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
            ErrorLabel.IsVisible = hasError;
        }

        private bool IsValid()
        {
            return Data.HasValue && Data.Value >= MinimumDate;
        }
    }
}
