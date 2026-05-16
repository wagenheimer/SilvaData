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
    private Parametro? _parametroInicial;
    private bool _hasAppeared;

    /// <summary>
    /// Construtor com DI e recebendo o Lote e opcionalmente o Parâmetro inicial a filtrar.
    /// O carregamento dos dados é iniciado em OnAppearing para garantir
    /// que a página já esteja na tela (evita tela branca no iOS).
    /// </summary>
    public LoteAvaliacaoGalpaoView(Lote lote, Parametro? parametro = null)
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteAvaliacaoGalpaoViewModel>();
        BindingContext = _viewModel;
        _lote = lote;
        _parametroInicial = parametro;

        // Sincroniza ParametroInicial no VM imediatamente para garantir consistência,
        // independente de quando CarregaDados é chamado (OnAppearing).
        if (parametro != null)
            _viewModel.ParametroInicial = parametro;

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
        Debug.WriteLine($"[LoteAvaliacaoGalpaoView] OnAppearing | _hasAppeared={_hasAppeared} | parametroInicial={_parametroInicial?.nome ?? "null"}");

        if (!_hasAppeared)
        {
            _hasAppeared = true;
            var loteToLoad = _lote;
            var parametroToLoad = _parametroInicial; // captura local — thread-safe
            if (loteToLoad != null)
            {
                // Inicia carregamento na main thread após a página estar visível.
                // Passa o parametroInicial diretamente para CarregaDados evitando
                // qualquer race condition com o estado do Singleton VM.
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Garante que o ParametroInicial está no VM antes de carregar
                        if (parametroToLoad != null)
                            _viewModel.ParametroInicial = parametroToLoad;

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
