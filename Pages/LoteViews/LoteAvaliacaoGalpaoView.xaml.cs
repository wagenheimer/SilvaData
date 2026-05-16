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
    private Lote? _lote;
    private bool _hasAppeared;

    /// <summary>
    /// Construtor com DI e recebendo o Lote.
    /// O carregamento dos dados é iniciado em OnAppearing para garantir
    /// que a página já esteja na tela (evita tela branca no iOS).
    /// </summary>
    public LoteAvaliacaoGalpaoView(Lote lote)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteAvaliacaoGalpaoViewModel>();
        BindingContext = _viewModel;
        _lote = lote;

        // Register for messages in Constructor to keep listening in background
        WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, (recipient, message) =>
        {
            Debug.WriteLine($"[LoteAvaliacaoGalpaoView] ★★★ FormularioSalvoMessage RECEBIDA");

            // Verifica se o formulário salvo pertence ao lote atual e é do tipo correto
            if (message.FormularioSalvo?.loteId == _viewModel.Lote?.id && message.FormularioSalvo?.parametroTipoId == 20)
            {
                Debug.WriteLine($"  ✓ Recarregando dados após salvar formulário");
                // Garante execução na main thread para evitar UIKit Consistency no iOS
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await _viewModel.CarregaFormulariosPreenchidos();
                        await _viewModel.CarregaAvaliacoes();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[LoteAvaliacaoGalpaoView] Erro ao recarregar após FormularioSalvo: {ex.Message}");
                    }
                });
            }
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine($"[LoteAvaliacaoGalpaoView] OnAppearing | _hasAppeared={_hasAppeared}");

        if (!_hasAppeared)
        {
            _hasAppeared = true;
            var loteToLoad = _lote;
            if (loteToLoad != null)
            {
                // Inicia carregamento na main thread após a página estar visível
                // evita tela branca no iOS ao usar fire-and-forget no construtor
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await _viewModel.CarregaDados(loteToLoad);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[LoteAvaliacaoGalpaoView] Erro em OnAppearing CarregaDados: {ex.Message}");
                    }
                });
            }
        }
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
