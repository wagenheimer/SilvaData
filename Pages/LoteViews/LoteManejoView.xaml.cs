using SilvaData.Models;
using SilvaData.ViewModels;
using SilvaData.Utilities;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// View para Manejo.
/// MIGRADO PARA MAUI: DI apenas para ViewModel.
/// </summary>
public partial class LoteManejoView : ContentPage, IDisposable
{
    private readonly LoteManejoViewModel _viewModel;

    /// <summary>
    /// ✅ Construtor com DI - APENAS ViewModel
    /// </summary>
    public LoteManejoView(Lote lote)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteManejoViewModel>();
        BindingContext = _viewModel;
               
        _= _viewModel.CarregaDados(lote); 
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
        System.Diagnostics.Debug.WriteLine($"[LoteManejoView] Dispose");
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.Cleanup();
    }
}
