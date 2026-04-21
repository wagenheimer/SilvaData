using SilvaData.Models;
using SilvaData.ViewModels;
using SilvaData.Utilities;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using SilvaData;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// View para Diagnóstico.
/// MIGRADO PARA MAUI: DI apenas para ViewModel.
/// </summary>
public partial class LoteDiagnosticoView : ContentPage, IDisposable
{
    private readonly LoteDiagnosticoViewModel _viewModel;
    private bool _jaFoiExibida;

    /// <summary>
    /// ✅ Construtor com DI - APENAS ViewModel
    /// </summary>
    public LoteDiagnosticoView(Lote lote)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteDiagnosticoViewModel>();
        BindingContext = _viewModel;
        
        _ = _viewModel.CarregaDados(lote);

    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_jaFoiExibida && _viewModel.Lote != null)
            _ = _viewModel.CarregaDados(_viewModel.Lote);

        _jaFoiExibida = true;
    }

    /// <summary>
    /// Back button handler.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        _ = NavigationUtils.PopModalAsync();
        return true;
    }

    /// <summary>
    /// Evento para o botão Adicionar Tratamento
    /// </summary>
    private async void AdicionarTratamento_Clicked(object sender, EventArgs e)
    {
        // Obter o DiagnosticoButton do binding context do botão
        if (sender is Syncfusion.Maui.Buttons.SfButton button && button.BindingContext is DiagnosticoButton diagnostico)
        {
            await _viewModel.AdicionarTratamento(diagnostico);
        }
        else
        {
            await App.Current.Windows[0].Page?.DisplayAlertAsync("Erro", "Não foi possível identificar o diagnóstico", "OK");
        }
    }

    /// <summary>
    /// Evento para o botão Ver Detalhes
    /// </summary>
    private async void VerDetalhes_Tapped(object sender, TappedEventArgs e)
    {
        // Obter o DiagnosticoButton do binding context do botão
        if (sender is Border border && border.BindingContext is DiagnosticoButton diagnostico)
        {
            await _viewModel.ShowDiagnostico(diagnostico);
        }
        else
        {
            await App.Current.MainPage.DisplayAlert("Erro", "Não foi possível identificar o diagnóstico", "OK");
        }
    }

    public void Dispose()
    {
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.Cleanup();
    }
}
