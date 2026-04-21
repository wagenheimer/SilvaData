using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// View para Vacinas.
/// MIGRADO PARA MAUI: DI apenas para ViewModel.
/// </summary>
public partial class LoteVacinasView : ContentPage, IDisposable
{
    private readonly LoteVacinasViewModel _viewModel;

    /// <summary>
    /// ✅ Construtor com DI - APENAS ViewModel
    /// </summary>
    public LoteVacinasView(Lote lote)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteVacinasViewModel>();
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
        System.Diagnostics.Debug.WriteLine($"[LoteVacinasView] Dispose");
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.Cleanup();
    }
}
