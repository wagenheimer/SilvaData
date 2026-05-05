using SilvaData.Models;
using SilvaData.Pages.PopUps;

namespace SilvaData.Controls;

public partial class LoteDescricaoLabel : Label
{
    public static readonly BindableProperty LoteProperty =
        BindableProperty.Create(nameof(Lote), typeof(Lote), typeof(LoteDescricaoLabel));

    public static readonly BindableProperty NomeTelaProperty =
        BindableProperty.Create(nameof(NomeTela), typeof(string), typeof(LoteDescricaoLabel), string.Empty);

    public Lote? Lote
    {
        get => (Lote?)GetValue(LoteProperty);
        set => SetValue(LoteProperty, value);
    }

    public string NomeTela
    {
        get => (string)GetValue(NomeTelaProperty);
        set => SetValue(NomeTelaProperty, value);
    }

    public LoteDescricaoLabel()
    {
        InitializeComponent();
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (Lote == null) return;
        await PopUpDetalhesLote.ShowAsync(Lote, NomeTela);
    }
}
