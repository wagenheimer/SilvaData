using CommunityToolkit.Mvvm.Messaging;
using SilvaData.ViewModels;
using SilvaData.Utilities;
using SilvaData.Models;
using System.Diagnostics;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// Página de avaliações no galpão.
/// MIGRADO PARA MAUI: DI, WeakReferenceMessenger.
/// </summary>
public partial class LoteAvaliacaoGalpaoView : ContentPage, IDisposable
{
    private readonly LoteAvaliacaoGalpaoViewModel _viewModel;
    /// <summary>
    /// Construtor com DI e recebendo o Lote.
    /// </summary>
    public LoteAvaliacaoGalpaoView(Lote lote)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteAvaliacaoGalpaoViewModel>();
        BindingContext = _viewModel;

        // Armazena lote para reload posterior
        _ = _viewModel.CarregaDados(lote);

        // Register for messages in Constructor to keep listening in background
        WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, (recipient, message) =>
        {
            Debug.WriteLine($"[LoteAvaliacaoGalpaoView] ★★★ FormularioSalvoMessage RECEBIDA");

            // Verifica se o formulário salvo pertence ao lote atual e é do tipo correto
            if (message.FormularioSalvo?.loteId == _viewModel.Lote?.id && message.FormularioSalvo?.parametroTipoId == 20)
            {
                Debug.WriteLine($"  ✓ Recarregando dados após salvar formulário");
                _ = Task.Run(async () =>
                {
                    await _viewModel.CarregaFormulariosPreenchidos();
                    await _viewModel.CarregaAvaliacoes();
                });
            }
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine($"[LoteAvaliacaoGalpaoView] OnAppearing");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Debug.WriteLine($"[LoteAvaliacaoGalpaoView] OnDisappearing");
    }

    protected override bool OnBackButtonPressed()
    {
        _ = NavigationUtils.PopModalAsync();
        return true;
    }

    public void Dispose()
    {
        Debug.WriteLine($"[LoteAvaliacaoGalpaoView] Dispose");
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.Cleanup();
    }
}
