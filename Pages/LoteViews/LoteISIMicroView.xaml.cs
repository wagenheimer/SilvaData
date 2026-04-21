using SilvaData.Models;
using SilvaData.ViewModels;
using SilvaData.Utilities;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// View para ISI Micro.
/// MIGRADO PARA MAUI: DI apenas para ViewModel.
/// </summary>
public partial class LoteISIMicroView : ContentPage, IDisposable
{
    private readonly LoteISIMicroViewModel _viewModel;

    /// <summary>
    /// ✅ Construtor com DI - APENAS ViewModel
    /// </summary>
    public LoteISIMicroView(Lote lote)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteISIMicroViewModel>();
        BindingContext = _viewModel;

        _ = _viewModel.CarregaDados(lote);

    }

    /// <summary>
    /// Carrega dados quando aparece.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // ✅ ViewModel já tem o Lote configurado antes da navegação
        // Não precisa chamar CarregaDados aqui
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
        System.Diagnostics.Debug.WriteLine($"[LoteISIMicroView] Dispose");
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.Cleanup();
    }
}
