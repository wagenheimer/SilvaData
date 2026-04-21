using SilvaData.Models;
using SilvaData.ViewModels;
using SilvaData.Utilities;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// View para Análise de Salmonella.
/// MIGRADO PARA MAUI: DI apenas para ViewModel.
/// </summary>
public partial class LoteSalmonellaView : ContentPage, IDisposable
{
    private readonly LoteSalmonellaViewModel _viewModel;

    /// <summary>
    /// ✅ Construtor com DI - APENAS ViewModel
    /// </summary>
    public LoteSalmonellaView(Lote lote)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteSalmonellaViewModel>();
        BindingContext = _viewModel;
        _ = _viewModel.CarregaDados(lote);
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
        System.Diagnostics.Debug.WriteLine($"[LoteSalmonellaView] Dispose");
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.Cleanup();
    }
}
