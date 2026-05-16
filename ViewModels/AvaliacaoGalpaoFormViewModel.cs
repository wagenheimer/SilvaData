using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utils;
using SilvaData.Utilities;

using HapticHelper = SilvaData.Utilities.HapticHelper;
using PopUpSuccessGalpao = SilvaData.Pages.PopUps.PopUpSuccessGalpao;
using PopUpErrorGalpao = SilvaData.Pages.PopUps.PopUpErrorGalpao;

using System.Collections.ObjectModel;
using System.Diagnostics;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SilvaData.ViewModels;

/// <summary>
/// ViewModel dedicado para Avaliação no Galpão (parametroTipoId == 20).
/// Singleton — SetInitialState + Cleanup chamados a cada abertura pela NavigationUtils.
/// </summary>
public partial class AvaliacaoGalpaoFormViewModel : ViewModelBase, ILoteFormImagemViewModel
{
    #region Observable Properties

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string subTitle = string.Empty;
    [ObservableProperty] private int loteId = -1;
    [ObservableProperty] private int loteFormId = -1;
    [ObservableProperty] private int? fase;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvaliacaoGalpaoQualitativo))]
    [NotifyPropertyChangedFor(nameof(AvaliacaoGalpaoQuantitativo))]
    [NotifyPropertyChangedFor(nameof(AvaliacaoGalpao))]
    private Parametro? parametroSelecionado;

    [ObservableProperty] private ObservableCollection<ParametroAlternativas> alternativasParametroSelecionado = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoteDescricaoCompleta))]
    private Lote? lote;

    [ObservableProperty] private int loteFormVinculado = -1;
    [ObservableProperty] private LoteFormulario? loteFormulario;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScoreTotalDaAveCor))]
    private int scoreTotalDaAve;

    [ObservableProperty] private bool podeEditar;
    [ObservableProperty] private new bool isReadOnly = true;
    [ObservableProperty] private Color realizadoColor = Colors.Blue;
    [ObservableProperty] private LoteFormImagem? loteFormImagem1;
    [ObservableProperty] private LoteFormImagem? loteFormImagem2;
    [ObservableProperty] private LoteFormImagem? loteFormImagem3;

    #endregion

    #region Computed Properties

    public bool AvaliacaoGalpao => AvaliacaoGalpaoQualitativo || AvaliacaoGalpaoQuantitativo;
    public bool AvaliacaoGalpaoQuantitativo => ParametroSelecionado != null && string.IsNullOrEmpty(ParametroSelecionado.campoTipo);
    public bool AvaliacaoGalpaoQualitativo => ParametroSelecionado != null && (ParametroSelecionado.campoTipo == "1" || ParametroSelecionado.campoTipo == "2");

    public string LoteDescricaoCompleta => Lote?.DescricaoCompleta ?? string.Empty;

    public Color ScoreTotalDaAveCor => ScoreTotalDaAve <= 21
        ? Color.FromArgb("#48ba00")
        : ScoreTotalDaAve <= 40
            ? Color.FromArgb("#ffba00")
            : Color.FromArgb("#fc4c17");

    #endregion

    #region Private Fields

    private bool _isLoadingAvaliacoes = false;
    private bool _autoSaveEnabled = false;
    private bool _internalMessagesRegistered;
    private CancellationTokenSource? _autoSaveCts;
    private const int AUTOSAVE_DEBOUNCE_MS = 500;
    private List<LoteFormAvaliacaoGalpao>? _subscribedAvaliacoesGalpao;

    public bool NovoFormulario { get; set; }

    #endregion

    #region Constructor

    public AvaliacaoGalpaoFormViewModel()
    {
        RegisterInternalMessages();
    }

    private void RegisterInternalMessages()
    {
        if (_internalMessagesRegistered) return;

        WeakReferenceMessenger.Default.Register<PropriedadeMudouMessage>(this, (r, m) =>
        {
            if (AvaliacaoGalpao)
                RecalculaTotaisAvaliacaoGalpao();
        });

        WeakReferenceMessenger.Default.Register<RecalcularAvaliacaoGalpaoMessage>(this, (r, m) =>
        {
            try
            {
                RecalculaTotaisAvaliacaoGalpao();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel] ❌ Erro ao recalcular: {ex.Message}");
            }
        });

        _internalMessagesRegistered = true;
    }

    #endregion

    #region Overrides

    protected override void OnIsBusyChanged()
    {
        base.OnIsBusyChanged();
        SalvarCommand.NotifyCanExecuteChanged();
        EditarCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Configura o estado inicial ANTES de abrir a página (chamado em NavigationUtils antes do PushModalAsync).
    /// Isso garante que IsBusy=true e ParametroSelecionado estejam corretos no primeiro render.
    /// </summary>
    public void SetInitialState(
        Lote lote,
        int loteFormId,
        int? fase,
        bool isReadOnly,
        bool podeEditar,
        Parametro? parametroSelecionado = null)
    {
        LoteId = (int)lote.id;
        LoteFormId = loteFormId;
        Fase = fase;
        IsReadOnly = isReadOnly;
        PodeEditar = podeEditar;
        Lote = lote;
        ParametroSelecionado = parametroSelecionado;

        LoteFormulario = new LoteFormulario();
        AlternativasParametroSelecionado.Clear();
        NovoFormulario = false;
        ScoreTotalDaAve = 0;

        Title = Traducao.AvaliacoesNoGalpao;
        SubTitle = Traducao.DetalhesDaAnálise;

        Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel] SetInitialState | LoteId={LoteId} | LoteFormId={LoteFormId} | ParametroSelecionado={ParametroSelecionado?.nome} | Qualitativo={AvaliacaoGalpaoQualitativo} | Quantitativo={AvaliacaoGalpaoQuantitativo}");
    }

    [RelayCommand]
    public async Task Carregar()
    {
        Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel] ▶ Carregar()");
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsBusy = true;
                SubTitle = Traducao.DetalhesDaAnálise;
            }).ConfigureAwait(false);

            await ConfigParametros.UpdateConfig().ConfigureAwait(false);

            bool novoLoteForm = LoteFormId == -1;

            LoteFormulario ??= new LoteFormulario();

            var loteResult = await Lote.PegaLoteAsync(LoteId).ConfigureAwait(false);
            loteResult?.EnsureNames();

            var loteFormResult = novoLoteForm
                ? null
                : await LoteForm.PegaFormulariosLotePorLoteFormId(LoteFormId).ConfigureAwait(false);

            List<LoteFormImagem>? imagensCarregadas = null;
            if (!novoLoteForm)
                imagensCarregadas = await LoteFormImagem.PegaImagens(LoteFormId).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Lote = loteResult;
                if (!novoLoteForm)
                {
                    LoteFormulario.LoteForm = loteFormResult;
                    LoteFormImagem1 = imagensCarregadas?.Count >= 1 ? imagensCarregadas[0] : null;
                    LoteFormImagem2 = imagensCarregadas?.Count >= 2 ? imagensCarregadas[1] : null;
                    LoteFormImagem3 = imagensCarregadas?.Count >= 3 ? imagensCarregadas[2] : null;
                }
            }).ConfigureAwait(false);

            // Se ParametroSelecionado ainda não foi definido, carrega do banco
            if (ParametroSelecionado == null)
            {
                var parametros = await Parametro.PegaParametroAsync(20).ConfigureAwait(false);
                if (parametros?.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        ParametroSelecionado = parametros.FirstOrDefault()).ConfigureAwait(false);
                    Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel] ParametroSelecionado carregado: {ParametroSelecionado?.nome}");
                }
            }

            await CarregaAvaliacaoGalpaoAsync(novoLoteForm);

            // Sincroniza LoteForm após carregar avaliações
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (LoteFormulario.LoteForm == null)
                {
                    InicializaNovoLoteForm();
                    NovoFormulario = true;
                }
                else
                {
                    NovoFormulario = false;
                }
            }).ConfigureAwait(false);

            _ = LoteFormulario?.SalvaEmAndamento();
            _autoSaveEnabled = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel] ✖ Erro em Carregar: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(() =>
                PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar: {ex.Message}"));
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsBusy = false;
                SalvarCommand.NotifyCanExecuteChanged();
                EditarCommand.NotifyCanExecuteChanged();
            }).ConfigureAwait(false);
        }
    }

    private void InicializaNovoLoteForm()
    {
        LoteFormulario!.LoteForm = new LoteForm
        {
            data = DateTime.Now,
            id = 0,
            loteId = LoteId,
            loteFormVinculado = LoteFormVinculado,
            parametroTipoId = 20,
            loteFormFase = Fase,
            dataInicioPreenchimento = DateTime.Now
        };
    }

    private async Task CarregaAvaliacaoGalpaoAsync(bool novoLoteForm)
    {
        if (_isLoadingAvaliacoes)
        {
            Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel.CarregaAvaliacaoGalpaoAsync] ⏸️ JÁ ESTÁ CARREGANDO");
            return;
        }

        _isLoadingAvaliacoes = true;

        try
        {
            LoteFormAvaliacaoGalpao.IsLoadingData = true;
            Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel.CarregaAvaliacaoGalpaoAsync] ParametroSelecionado={ParametroSelecionado?.nome}");

            if (LoteFormulario!.ListaAvaliacoesGalpao.Any())
            {
                Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel.CarregaAvaliacaoGalpaoAsync] ✓ Já possui dados - Ignorando");
                return;
            }

            if (ParametroSelecionado == null)
            {
                Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel.CarregaAvaliacaoGalpaoAsync] ✖ ParametroSelecionado é NULL!");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AlternativasParametroSelecionado.Clear();
                    LoteFormulario.ListaAvaliacoesGalpao.Clear();
                }).ConfigureAwait(false);
                return;
            }

            var alternativas = await ParametroAlternativas.PegaAlternativas(ParametroSelecionado.id);
            var avaliacoes = novoLoteForm
                ? new List<LoteFormAvaliacaoGalpao>()
                : await LoteFormAvaliacaoGalpao.RespostasAvaliacaoGalpaoPorLote(LoteFormId);

            Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel] Alternativas={alternativas?.Count ?? 0}, Avaliações={avaliacoes?.Count ?? 0}");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AlternativasParametroSelecionado.Clear();
                if (alternativas != null)
                    foreach (var alt in alternativas)
                        AlternativasParametroSelecionado.Add(alt);

                var novaLista = new List<LoteFormAvaliacaoGalpao>();

                if (novoLoteForm)
                {
                    var qtdMaxima = ParametroSelecionado?.qtdCampos ?? ParametroSelecionado?.qtdMinima ?? 1;
                    for (int i = 1; i <= qtdMaxima; i++)
                    {
                        novaLista.Add(new LoteFormAvaliacaoGalpao
                        {
                            NumeroResposta = i,
                            parametroId = ParametroSelecionado!.id,
                            Parametro = ParametroSelecionado,
                            ParametroAlternativas = AlternativasParametroSelecionado,
                            LoteFormId = LoteFormId == -1 ? null : LoteFormId
                        });
                    }
                }
                else
                {
                    // Adiciona as já existentes
                    foreach (var av in avaliacoes)
                    {
                        av.ParametroAlternativas = AlternativasParametroSelecionado;
                        av.Parametro = ParametroSelecionado;
                        novaLista.Add(av);
                    }

                    // Se for quantitativo, garante que tem o número correto de campos (padding)
                    if (AvaliacaoGalpaoQuantitativo)
                    {
                        var qtdMaxima = ParametroSelecionado?.qtdCampos ?? ParametroSelecionado?.qtdMinima ?? 1;
                        for (int i = novaLista.Count + 1; i <= (int)qtdMaxima; i++)
                        {
                            novaLista.Add(new LoteFormAvaliacaoGalpao
                            {
                                NumeroResposta = i,
                                parametroId = ParametroSelecionado!.id,
                                Parametro = ParametroSelecionado,
                                ParametroAlternativas = AlternativasParametroSelecionado,
                                LoteFormId = LoteFormId
                            });
                        }
                    }
                }

                LoteFormulario.ListaAvaliacoesGalpao = novaLista;
                AttachAvaliacoesGalpaoSubscription(novaLista);

                Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel] ListaAvaliacoesGalpao.Count={novaLista.Count}");
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel.CarregaAvaliacaoGalpaoAsync] ❌ Erro: {ex.Message}");
        }
        finally
        {
            LoteFormAvaliacaoGalpao.IsLoadingData = false;
            _isLoadingAvaliacoes = false;
        }
    }

    #endregion

    #region Auto-save Subscriptions

    private void AttachAvaliacoesGalpaoSubscription(List<LoteFormAvaliacaoGalpao>? avaliacoes)
    {
        DetachAvaliacoesGalpaoSubscription();
        _subscribedAvaliacoesGalpao = avaliacoes;
        if (_subscribedAvaliacoesGalpao == null) return;
        foreach (var avaliacao in _subscribedAvaliacoesGalpao)
            avaliacao.PropertyChanged += OnGalpaoDataChanged;
    }

    private void DetachAvaliacoesGalpaoSubscription()
    {
        if (_subscribedAvaliacoesGalpao == null) return;
        foreach (var avaliacao in _subscribedAvaliacoesGalpao)
            avaliacao.PropertyChanged -= OnGalpaoDataChanged;
        _subscribedAvaliacoesGalpao = null;
    }

    private void OnGalpaoDataChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_autoSaveEnabled || LoteFormAvaliacaoGalpao.IsLoadingData) return;
        if (e.PropertyName == nameof(LoteFormAvaliacaoGalpao.RespostaQtde) ||
            e.PropertyName == nameof(LoteFormAvaliacaoGalpao.AlternativaIdsJson))
        {
            _ = SalvaEmAndamentoDebounced();
        }
    }

    private async Task SalvaEmAndamentoDebounced()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(AUTOSAVE_DEBOUNCE_MS, _autoSaveCts.Token);
            _ = LoteFormulario?.SalvaEmAndamento();
        }
        catch (TaskCanceledException) { }
    }

    #endregion

    #region Recálculo de Totais

    private void RecalculaTotaisAvaliacaoGalpao()
    {
        if (LoteFormulario?.ListaAvaliacoesGalpao == null || !AvaliacaoGalpao)
            return;

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoteFormulario.AtualizouDados();

                ScoreTotalDaAve = AvaliacaoGalpaoQuantitativo
                    ? LoteFormulario.AvaliacoesRealizadasQuantitativa
                    : LoteFormulario.AvaliacoesRealizadasQualitativa;

                int min = ParametroSelecionado?.qtdMinima ?? 0;
                int max = ParametroSelecionado?.qtdCampos ?? 0;
                bool isForaDosLimites = (ScoreTotalDaAve < min) || (max > 0 && ScoreTotalDaAve > max);

                if (isForaDosLimites)
                {
                    RealizadoColor = Colors.Red;
                }
                else
                {
                    if (Application.Current?.Resources.TryGetValue("SecondaryColor", out var color) == true && color is Color c)
                        RealizadoColor = c;
                    else
                        RealizadoColor = Colors.Blue;
                }

                OnPropertyChanged(nameof(ScoreTotalDaAve));
                OnPropertyChanged(nameof(ScoreTotalDaAveCor));
                OnPropertyChanged(nameof(RealizadoColor));
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel] ❌ Erro RecalculaTotais: {ex.Message}");
        }
    }

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(NotBusy), AllowConcurrentExecutions = false)]
    public async Task Salvar()
    {
        if (LoteFormulario == null || LoteFormulario.LoteForm == null)
            return;

        try
        {
            if (NovoFormulario)
                LoteFormulario.LoteForm.dataTerminoPreenchimento = DateTime.Now;

            if (AvaliacaoGalpaoQuantitativo && LoteFormulario.AvaliacoesRealizadasQuantitativa < ParametroSelecionado!.qtdMinima)
            {
                await MostraErroAvaliacao((int)ParametroSelecionado.qtdMinima, LoteFormulario.AvaliacoesRealizadasQuantitativa);
                return;
            }

            if (AvaliacaoGalpaoQualitativo && LoteFormulario.AvaliacoesRealizadasQualitativa < ParametroSelecionado!.qtdMinima)
            {
                await MostraErroAvaliacao((int)ParametroSelecionado.qtdMinima, LoteFormulario.AvaliacoesRealizadasQualitativa);
                return;
            }

            IsBusy = true;
            await LoteForm.SalvaLoteFormularioAsync(LoteFormulario?.LoteForm);

            var loteFormIdSalvo = LoteFormulario!.LoteForm!.id ?? 0;

            var avaliacoes = LoteFormulario!.ListaAvaliacoesGalpao
                .Where(a => a.TemAlternativaSelecionada)
                .ToList();

            await Db.ExecuteAsync("DELETE FROM LoteFormAvaliacaoGalpao WHERE LoteFormId = ? AND parametroId = ?", loteFormIdSalvo, ParametroSelecionado!.id);

            foreach (var avaliacao in avaliacoes)
            {
                avaliacao.LoteFormId = loteFormIdSalvo;
                avaliacao.id = 0;
                await Db.InsertAsync(avaliacao);
            }

            await LoteFormImagem.DeletaImagens(LoteFormulario?.LoteForm);
            var imagensTasks = new List<Task>();
            if (LoteFormImagem1 != null) { LoteFormImagem1.LoteFormId = LoteFormulario?.LoteForm.id ?? 0; imagensTasks.Add(LoteFormImagem.AdicionaImagem(LoteFormImagem1)); }
            if (LoteFormImagem2 != null) { LoteFormImagem2.LoteFormId = LoteFormulario?.LoteForm.id ?? 0; imagensTasks.Add(LoteFormImagem.AdicionaImagem(LoteFormImagem2)); }
            if (LoteFormImagem3 != null) { LoteFormImagem3.LoteFormId = LoteFormulario?.LoteForm.id ?? 0; imagensTasks.Add(LoteFormImagem.AdicionaImagem(LoteFormImagem3)); }
            if (imagensTasks.Any()) await Task.WhenAll(imagensTasks);

            LoteFormulario.ApagaEmAndamento();
            Salvou = true;
            HapticHelper.VibrateSuccess();

            WeakReferenceMessenger.Default.Send(new FormularioSalvoMessage(LoteFormulario.LoteForm!));

            if (NovoFormulario)
            {
                int min = ParametroSelecionado?.qtdMinima ?? 0;
                int max = ParametroSelecionado?.qtdCampos ?? 0;
                int realizado = ScoreTotalDaAve;
                double media = AvaliacaoGalpaoQuantitativo ? LoteFormulario.AvaliacaoMediaQuantitativa : 0;

                string msgDescarte = "";
                int totalRegistrosDefinidos = LoteFormulario.ListaAvaliacoesGalpao.Count;

                if (realizado < totalRegistrosDefinidos)
                {
                    int descartados = totalRegistrosDefinidos - realizado;
                    msgDescarte = descartados == 1
                        ? string.Format(Traducao.UltimaNaoFoiPreenchido, totalRegistrosDefinidos)
                        : string.Format(Traducao.RegistrosNaoPreenchidosDescartados, descartados);
                }

                await PopUpSuccessGalpao.ShowAsync(
                    Traducao.AvaliacoesNoGalpao,
                    Traducao.AdicionadoComSucesso,
                    min, realizado, max, media,
                    AvaliacaoGalpaoQuantitativo,
                    RealizadoColor,
                    msgDescarte);
            }
            else
            {
                await PopUpOK.ShowAsync(Title, Traducao.AsAlteraçõesForamSalvas);
            }

            await Voltar();
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao salvar: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task MostraErroAvaliacao(int minimo, int realizado)
    {
        int max = ParametroSelecionado?.qtdCampos ?? 0;
        double media = 0;

        if (AvaliacaoGalpaoQuantitativo)
        {
            var valoresPreenchidos = LoteFormulario!.ListaAvaliacoesGalpao
                .Where(av => av.RespostaQtde.HasValue)
                .Select(av => av.RespostaQtde.Value)
                .ToList();
            if (valoresPreenchidos.Any())
                media = valoresPreenchidos.Average();
        }

        await PopUpErrorGalpao.ShowAsync(
            Traducao.Atenção,
            "Para salvar esta avaliação, você deve preencher pelo menos o número mínimo de registros exigido.",
            minimo, realizado, max, media, AvaliacaoGalpaoQuantitativo);
    }

    [RelayCommand(CanExecute = nameof(NotBusy))]
    public async Task Editar()
    {
        IsBusy = true;
        IsReadOnly = false;
        Salvou = false;
        IsBusy = false;
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task ProcurarAvaliacoes()
    {
        try
        {
            if (LoteFormulario?.ListaAvaliacoesGalpao == null || LoteFormulario.ListaAvaliacoesGalpao.Count == 0)
            {
                await PopUpOK.ShowAsync(Traducao.Atenção, "Nenhum registro encontrado para visualizar.");
                return;
            }

            var registroSelecionado = await VerRegistrosPopup.ShowAsync(
                new ObservableCollection<LoteFormAvaliacaoGalpao>(LoteFormulario.ListaAvaliacoesGalpao),
                AvaliacaoGalpaoQualitativo);

            if (registroSelecionado == null)
                return;

            if (AvaliacaoGalpaoQualitativo)
            {
                if (registroSelecionado.ParametroAlternativas != null)
                {
                    AlternativasParametroSelecionado = registroSelecionado.ParametroAlternativas;
                    WeakReferenceMessenger.Default.Send(new SelecionouAvaliacaoQualitativaMessage(registroSelecionado));
                }
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    WeakReferenceMessenger.Default.Send(new NavigateToRegistroMessage(registroSelecionado)));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AvaliacaoGalpaoFormViewModel] ❌ Erro ProcurarAvaliacoes: {ex.Message}");
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao abrir registros: {ex.Message}");
        }
    }

    #endregion

    #region Navigation

    public override async Task Voltar()
    {
        if (IsReadOnly || Salvou)
        {
            await base.Voltar();
            return;
        }

        if (await NavigationUtils.PopModalSeConfirmar())
            LoteFormulario?.ApagaEmAndamento();
    }

    #endregion

    #region Cleanup

    public override void Cleanup()
    {
        base.Cleanup();

        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
        _autoSaveCts = null;

        _isLoadingAvaliacoes = false;
        _autoSaveEnabled = false;

        DetachAvaliacoesGalpaoSubscription();

        LoteFormulario = null;
        AlternativasParametroSelecionado.Clear();
        ScoreTotalDaAve = 0;
        IsReadOnly = false;
        LoteFormId = -1;
        LoteFormVinculado = -1;
        ParametroSelecionado = null;
        NovoFormulario = false;
        LoteFormImagem1 = null;
        LoteFormImagem2 = null;
        LoteFormImagem3 = null;
    }

    #endregion
}
