using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Controls;
using SilvaData.Models;
using SilvaData.Utils;
using SilvaData.ViewModels;

using System.Diagnostics;

namespace SilvaData.Pages.LoteViews;

/// <summary>
/// Página dedicada para Avaliação no Galpão (parametroTipoId == 20).
/// Separada do LoteFormularioView para evitar problemas de sincronização
/// das alternativas qualitativas e do indicador de loading.
/// </summary>
public partial class AvaliacaoGalpaoFormView : ContentPage
{
    private readonly AvaliacaoGalpaoFormViewModel _vm;
    private bool _isInitialized = false;
    private bool _isBusyHandlerSubscribed = false;

    public AvaliacaoGalpaoFormView(AvaliacaoGalpaoFormViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        BindingContext = _vm;

        _vm.IsBusyChanged += OnIsBusyChangedRefreshButtons;
        _isBusyHandlerSubscribed = true;
    }

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_isInitialized && !LoteFormularioView.CarregandoFoto)
        {
            _isInitialized = true;
            RegisterMessages();
            _ = LoadAndSyncAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (!NavigationUtils.TemModalAberta(this) && !LoteFormularioView.CarregandoFoto)
        {
            if (_isBusyHandlerSubscribed)
            {
                _vm.IsBusyChanged -= OnIsBusyChangedRefreshButtons;
                _isBusyHandlerSubscribed = false;
            }
            UnregisterMessages();
            _isInitialized = false;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        Helpers.MostraConfirmacaodePopModal();
        return true;
    }

    #endregion

    #region Loading

    private async Task LoadAndSyncAsync()
    {
        await Task.Yield();

        // iOS needs a tiny bit of time for native control initialization
        if (DeviceInfo.Platform == DevicePlatform.iOS)
            await Task.Delay(50);

        InjectHeavyTemplates();

        // Awaits full data loading including UI thread synchronization in VM
        await _vm.Carregar().ConfigureAwait(false);

        // Syncs alternatives UI only if it is a qualitative assessment
        if (_vm.AvaliacaoGalpaoQualitativo)
        {
            await SyncAvaliacaoAlternativasAsync().ConfigureAwait(false);
        }
    }

    private void InjectHeavyTemplates()
    {
        if (!_vm.AvaliacaoGalpaoQuantitativo) return;

        try
        {
            camposaPreencherGalpao.FooterTemplate = (DataTemplate)Resources["GalpaoFooterTemplate"];
            camposaPreencherGalpao.AutoFitMode = Syncfusion.Maui.ListView.AutoFitMode.DynamicHeight;
            camposaPreencherGalpao.CachingStrategy = Syncfusion.Maui.ListView.CachingStrategy.RecycleTemplate;
            Debug.WriteLine("[AvaliacaoGalpaoFormView] Templates injetados");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AvaliacaoGalpaoFormView] ❌ Erro ao injetar templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Sincroniza o AvaliacaoAlternativasViewModel (singleton compartilhado) com os dados
    /// carregados pelo ViewModel dedicado.
    /// O InvokeOnMainThreadAsync funciona como ponto de sincronização com os BeginInvokeOnMainThread
    /// pendentes de CarregaAvaliacaoGalpaoAsync, garantindo que os dados já foram populados.
    /// </summary>
    private async Task SyncAvaliacaoAlternativasAsync()
    {
        try
        {
            // Retries a few times if the control is not yet ready (common on iOS)
            AvaliacaoAlternativasViewModel? avaliacaoVM = null;
            for (int i = 0; i < 5; i++)
            {
                avaliacaoVM = await MainThread.InvokeOnMainThreadAsync(() => avaliacaoAlternativas?.ViewModel);
                if (avaliacaoVM != null) break;
                
                Debug.WriteLine($"[AvaliacaoGalpaoFormView] ⚠️ AvaliacaoAlternativas.ViewModel ainda null — retry {i+1}/5");
                await Task.Delay(100);
            }

            if (avaliacaoVM == null)
            {
                Debug.WriteLine("[AvaliacaoGalpaoFormView] ❌ AvaliacaoAlternativas.ViewModel não disponível após retries");
                return;
            }

            // Lê dados na main thread — BeginInvokeOnMainThread de CarregaAvaliacaoGalpaoAsync
            // já executou (sincronizado pelo InvokeOnMainThreadAsync acima).
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var avaliacoes = _vm.LoteFormulario?.ListaAvaliacoesGalpao?.ToList()
                    ?? new List<LoteFormAvaliacaoGalpao>();
                var alternativas = _vm.AlternativasParametroSelecionado?.ToList()
                    ?? new List<ParametroAlternativas>();

                Debug.WriteLine($"[AvaliacaoGalpaoFormView] Sincronizando: {avaliacoes.Count} avaliações, {alternativas.Count} alternativas");

                avaliacaoVM.ListaAvaliacoesGalpao.Clear();
                avaliacaoVM.AlternativasParametroSelecionado.Clear();

                foreach (var av in avaliacoes)
                    avaliacaoVM.ListaAvaliacoesGalpao.Add(av);

                foreach (var alt in alternativas)
                    avaliacaoVM.AlternativasParametroSelecionado.Add(alt);

                if (alternativas.Any())
                {
                    avaliacaoVM.AtualizaTotalLiberado();
                    avaliacaoVM.AtualizaComboBoxLista();
                }

                avaliacaoVM.PodeSelecionarMaisQueUm = _vm.ParametroSelecionado?.campoTipo == "2";
                var qtdMax = _vm.ParametroSelecionado?.qtdCampos ?? _vm.ParametroSelecionado?.qtdMinima ?? 1;
                avaliacaoVM.ConfigurarQuantidadeMaxima(qtdMax);

                Debug.WriteLine("[AvaliacaoGalpaoFormView] ✅ AvaliacaoAlternativasViewModel sincronizado");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AvaliacaoGalpaoFormView] ❌ Erro SyncAvaliacaoAlternativasAsync: {ex.Message}");
        }
    }

    #endregion

    #region Messages

    private void RegisterMessages()
    {
        UnregisterMessages();

        WeakReferenceMessenger.Default.Register<NavigateToRegistroMessage>(this, (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (camposaPreencherGalpao?.Handler != null)
                        camposaPreencherGalpao.ScrollTo(m.Registro, ScrollToPosition.Center, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AvaliacaoGalpaoFormView] ❌ Erro scroll: {ex.Message}");
                }
            });
        });
    }

    private void UnregisterMessages()
    {
        try
        {
            WeakReferenceMessenger.Default.Unregister<NavigateToRegistroMessage>(this);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AvaliacaoGalpaoFormView] Erro UnregisterMessages: {ex.Message}");
        }
    }

    #endregion

    #region Button State

    private void OnIsBusyChangedRefreshButtons(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (btSalvar == null || btSalvar.Handler == null) return;
                if (!_vm.IsBusy)
                    VisualStateManager.GoToState(btSalvar, "Normal");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AvaliacaoGalpaoFormView] ❌ Erro refresh botão: {ex.Message}");
            }
        });
    }

    #endregion
}
