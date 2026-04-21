using CommunityToolkit.Mvvm.Input;
using SilvaData.Utilities;
using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;


namespace SilvaData.Controls
{
    public partial class ISINumericUpDown : ContentView
    {
        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(int?), typeof(ISINumericUpDown), null, BindingMode.TwoWay, propertyChanged: OnValueChanged);

        public static readonly BindableProperty MinimumProperty =
            BindableProperty.Create(nameof(Minimum), typeof(double?), typeof(ISINumericUpDown), null);

        public static readonly BindableProperty MaximumProperty =
            BindableProperty.Create(nameof(Maximum), typeof(double?), typeof(ISINumericUpDown), null);

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(ISINumericUpDown), false, propertyChanged: OnIsReadOnlyChanged);

        public static readonly BindableProperty TextColorProperty =
            BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(ISINumericUpDown), Colors.Black);

        public static readonly BindableProperty FontSizeProperty =
            BindableProperty.Create(nameof(FontSize), typeof(double), typeof(ISINumericUpDown), 18.0);

        public static readonly BindableProperty FontAttributesProperty =
            BindableProperty.Create(nameof(FontAttributes), typeof(FontAttributes), typeof(ISINumericUpDown), FontAttributes.None);

        public int? Value
        {
            get => (int?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double? Minimum
        {
            get => (double?)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double? Maximum
        {
            get => (double?)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public FontAttributes FontAttributes
        {
            get => (FontAttributes)GetValue(FontAttributesProperty);
            set => SetValue(FontAttributesProperty, value);
        }

        public bool IsNotReadOnly => !IsReadOnly;

        public string ValueText
        {
            get => Value?.ToString() ?? string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (Value != null)
                        Value = null;
                    return;
                }

                if (int.TryParse(value, out int parsed))
                {
                    if (Minimum.HasValue && parsed < (int)Math.Round(Minimum.Value)) parsed = (int)Math.Round(Minimum.Value);
                    if (Maximum.HasValue && parsed > (int)Math.Round(Maximum.Value)) parsed = (int)Math.Round(Maximum.Value);

                    if (Value != parsed)
                        Value = parsed;
                }
            }
        }

        public ISINumericUpDown()
        {
            InitializeComponent();
        }

        private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ISINumericUpDown control)
            {
                control.OnPropertyChanged(nameof(ValueText));
            }
        }

        private static void OnIsReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ISINumericUpDown control)
            {
                control.OnPropertyChanged(nameof(IsNotReadOnly));
            }
        }

        [RelayCommand]
        private void Plus()
        {
            if (IsReadOnly) return;
            
            try
            {
                HapticHelper.VibrateClick();
            }
            catch { /* Ignored */ }

            int current = Value ?? (Minimum.HasValue ? (int)Math.Round(Minimum.Value) : 0);
            int next = current + 1;

            if (Maximum.HasValue && next > (int)Math.Round(Maximum.Value))
                next = (int)Math.Round(Maximum.Value);

            Value = next;
        }

        [RelayCommand]
        private void Minus()
        {
            if (IsReadOnly) return;
            
            try
            {
                HapticHelper.VibrateClick();
            }
            catch { /* Ignored */ }

            int current = Value ?? (Minimum.HasValue ? (int)Math.Round(Minimum.Value) : 0);
            int next = current - 1;

            if (Minimum.HasValue && next < (int)Math.Round(Minimum.Value))
                next = (int)Math.Round(Minimum.Value);

            Value = next;
        }
    }
}
