using SilvaData.Models;
using SilvaData.ViewModels;
using SilvaData.Utilities;
using SilvaData.Utils;
using System.Diagnostics;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// View para Nutrição.
/// ✅ Integrado com tracking de modais para singleton.
/// </summary>
public partial class LoteNutricaoView : ContentPage, IDisposable
{
    private readonly LoteNutricaoViewModel _viewModel;
    private Lote? _loteInicial;

    /// <summary>
    /// ✅ Construtor com DI
    /// </summary>
    public LoteNutricaoView(Lote lote)
    {
        InitializeComponent();

        _viewModel = ServiceHelper.GetRequiredService<LoteNutricaoViewModel>();
        BindingContext = _viewModel;

        _loteInicial = lote;
    }

    /// <summary>
    /// ✅ Detecta retorno de modal e não recarrega
    /// </summary>
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        try
        {
            // ✅ Se está retornando de modal/popup, apenas recarrega a lista
            if (NavigationUtils.TemModalAberta(this))
            {
                Debug.WriteLine("[LoteNutricaoView] ⏸️ Retornando de modal - recarrega lista");

                // ✅ Recarrega apenas os dados, não toda a página
                _ = MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (_viewModel.Lote != null)
                    {
                        await _viewModel.CarregaDados(_viewModel.Lote);
                    }
                });

                return;
            }

            Debug.WriteLine("[LoteNutricaoView] ▶️ Navegação real - inicializando");

            // ✅ Primeira vez ou navegação real - carrega completo
            if (_loteInicial != null)
            {
                _ = MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _viewModel.CarregaDados(_loteInicial);
                    _loteInicial = null; // ✅ Usa apenas uma vez
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteNutricaoView] Erro OnNavigatedTo: {ex.Message}");
        }
    }

    /// <summary>
    /// Back button handler.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        _ = NavigationUtils.PopModalAsync();
        return true;
    }

    public void Dispose()
    {
        System.Diagnostics.Debug.WriteLine($"[LoteNutricaoView] Dispose");
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.Cleanup();
    }
}
