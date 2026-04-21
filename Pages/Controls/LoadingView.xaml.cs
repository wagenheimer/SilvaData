using SilvaData.Utilities;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SilvaData.Controls;

public partial class LoadingView : ContentView
{
    // Use defaultValueCreator to resolve app resources at runtime (no static access to Resources).
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(LoadingView), string.Empty);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(LoadingView),
            defaultValueCreator: _ => ISIUtils.ResolveColor("PrimaryColor", Colors.White));

    public static readonly BindableProperty IndicatorColorProperty =
        BindableProperty.Create(
            nameof(IndicatorColor),
            typeof(Color),
            typeof(LoadingView),
            defaultValueCreator: _ => ISIUtils.ResolveColor("PrimaryColor", Colors.White));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public Color IndicatorColor
    {
        get => (Color)GetValue(IndicatorColorProperty);
        set => SetValue(IndicatorColorProperty, value);
    }

    public LoadingView()
    {
        InitializeComponent();
    }

}
