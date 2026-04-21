namespace SilvaData.Controls
{
    public partial class sfFrameLabel : ContentView
    {
        // BindableProperties (Structure is MAUI compliant)
        public static readonly BindableProperty HintProperty =
            BindableProperty.Create(nameof(Hint), typeof(string), typeof(sfFrameLabel), string.Empty);

        public static readonly BindableProperty IconProperty =
            BindableProperty.Create(nameof(Icon), typeof(string), typeof(sfFrameLabel), string.Empty);

        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(nameof(Text), typeof(string), typeof(sfFrameLabel), string.Empty);

        // Property Wrappers
        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public sfFrameLabel()
        {
            InitializeComponent();
        }
    }
}