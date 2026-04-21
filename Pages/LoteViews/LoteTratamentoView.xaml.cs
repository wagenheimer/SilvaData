using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// View para Tratamento via Água.
/// MIGRADO PARA MAUI: DI apenas para ViewModel.
/// </summary>
public partial class LoteTratamentoView : ContentPage, IDisposable
{
    private readonly LoteTratamentoViewModel _viewModel;

    /// <summary>
    /// ✅ Construtor com DI - APENAS ViewModel
    /// </summary>
    public LoteTratamentoView(Lote lote)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteTratamentoViewModel>();
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
        System.Diagnostics.Debug.WriteLine($"[LoteTratamentoView] Dispose");
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.Cleanup();
    }
}
