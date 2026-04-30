using System.Diagnostics;

namespace SilvaData.Controls
{
    public partial class ISIDoubleValue : ValidatableFieldBase
    {
        // BindableProperties
        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(
                nameof(Value),
                typeof(double?),
                typeof(ISIDoubleValue),
                null,
                BindingMode.TwoWay,
                propertyChanged: (bindable, _, __) => 
                {
                    var control = (ISIDoubleValue)bindable;
                    control.OnPropertyChanged(nameof(ShowRequiredStar));
                    control.ScheduleValidationRefresh();
                }});

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(ISIDoubleValue), string.Empty);

        public static readonly BindableProperty HelperTextProperty =
            BindableProperty.Create(nameof(HelperText), typeof(string), typeof(ISIDoubleValue), string.Empty);

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(ISIDoubleValue), false);

        public static readonly BindableProperty IsObrigatorioProperty =
            BindableProperty.Create(
                nameof(IsObrigatorio), 
                typeof(bool), 
                typeof(ISIDoubleValue), 
                false,
                propertyChanged: (bindable, oldValue, newValue) => 
                {
                    if (bindable is ISIDoubleValue control)
                    {
                        control.OnPropertyChanged(nameof(ShowRequiredStar));
                    }
                });

        // Property Wrappers
        public double? Value
        {
            get => (double?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string HelperText
        {
            get => (string)GetValue(HelperTextProperty);
            set => SetValue(HelperTextProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool IsObrigatorio
        {
            get => (bool)GetValue(IsObrigatorioProperty);
            set => SetValue(IsObrigatorioProperty, value);
        }

        /// <summary>
        /// Define se o asterisco de campo obrigatório deve ser exibido.
        /// Visível apenas se for obrigatório E ainda não estiver preenchido.
        /// </summary>
        public bool ShowRequiredStar => IsObrigatorio && !Value.HasValue;

        public ISIDoubleValue()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] ISIDoubleValue: {ex.Message}");
                throw;
            }
        }

        protected override void OnContextAttached()
        {
        }

        protected override void OnContextCleared()
        {
            Value = null;
            HasError = false;
        }

        protected override bool ComputeHasError()
        {
            bool campoVazio = !Value.HasValue;
            return IsObrigatorio && campoVazio && IsAnyValidationActive && !IsReadOnly;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
        }
    }
}
