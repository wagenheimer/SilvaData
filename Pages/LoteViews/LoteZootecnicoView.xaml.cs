using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// View para Zootécnico.
/// MIGRADO PARA MAUI: DI apenas para ViewModel.
/// </summary>
public partial class LoteZootecnicoView : ContentPage, IDisposable
{
    private readonly LoteZootecnicoViewModel _viewModel;

    /// <summary>
    /// ✅ Construtor com DI - APENAS ViewModel
    /// </summary>
    public LoteZootecnicoView(Lote lote)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteZootecnicoViewModel>();
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
        System.Diagnostics.Debug.WriteLine($"[LoteZootecnicoView] Dispose");
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.Cleanup();
    }
}
