using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using SilvaData.Utils;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.ViewModels;

public enum BoxStatus { EmAndamento, JaPreenchido, AindaNaoChegou, Atrazado }

public partial class ManejoZootecnicoBox : ObservableObject
{
    public int DiasDe { get; set; }
    public int DiasAte { get; set; } = -9999;
    public string? Nome { get; set; }
    public int Numero { get; set; }
    public int? Fase { get; set; }
    public int TipoParametro { get; set; }
    public BoxStatus BoxStatus { get; set; }
    public LoteForm? LoteForm { get; set; }
    public bool IsPersonalizado { get; set; } // ★ NOVO: Identifica manejos personalizados

    [ObservableProperty] private string? dataCaixaText;
    [ObservableProperty] private string? dataPreenchimento;
    [ObservableProperty] private Color iconColor;
    [ObservableProperty] private Color backgroundColor;
    [ObservableProperty] private Color fontColor;
    [ObservableProperty] private string? icon;
    [ObservableProperty] private float opacity = 1f;

    // ★ NOVO: Comando para navegação (padrão do projeto)
    public IRelayCommand? ShowManejoCommand { get; set; }

    public string NomeCaixa => string.IsNullOrEmpty(Nome) ? $"{Numero:D2} {Traducao.Dias}" : Nome;

    /// <summary>
    /// Carrega os dados do formulário para esta caixa.
    /// </summary>
    public async Task PegaDados(Lote lote, bool podeEditar, bool podeVer)
    {
        if (lote?.dataInicio == null) return;

        var dataInicio = (DateTime)lote.dataInicio;
        LoteForm = await LoteForm.PegaFormulariosLote((int)lote.id, TipoParametro, Fase, null);

        var dataFormulario = LoteForm?.data ?? DateTime.MinValue;

        DataCaixaText = DiasAte != -9999
            ? $"{dataInicio.AddDays(DiasDe).ToShortDateString()} {Traducao.Até} {dataInicio.AddDays(DiasAte).ToShortDateString()}"
            : $"{Traducao.APartirDe} {dataInicio.AddDays(DiasDe).ToShortDateString()}";

        // Atualiza status visual
        if (LoteForm != null)
        {
            DataPreenchimento = dataFormulario.ToShortDateString();
            BoxStatus = BoxStatus.JaPreenchido;
            Icon = FontAwesome.FontAwesomeIcons.SquareCheck;
            BackgroundColor = Color.FromArgb("#48BA00");
            FontColor = Colors.White;
            IconColor = Colors.White;
            Opacity = 1f;
        }
        else if (dataInicio.AddDays(DiasDe) > DateTime.Now)
        {
            BoxStatus = BoxStatus.AindaNaoChegou;
            Icon = FontAwesome.FontAwesomeIcons.Clock;
            AtualizaCoresInativa(podeVer);
        }
        else if (dataInicio.AddDays(DiasAte) < DateTime.Now && DiasAte != -9999)
        {
            BoxStatus = BoxStatus.Atrazado;
            Icon = FontAwesome.FontAwesomeIcons.PenToSquare;
            IconColor = Color.FromArgb("#FC6D42");
            AtualizaCoresInativa(podeVer);
        }
        else
        {
            BoxStatus = BoxStatus.EmAndamento;
            Icon = FontAwesome.FontAwesomeIcons.Hourglass;
            IconColor = Color.FromArgb("#FFBA00");
            AtualizaCoresInativa(podeVer);
        }
    }

    // ✅ Extrai lógica repetida
    private void AtualizaCoresInativa(bool podeVer)
    {
        BackgroundColor = Colors.White;
        FontColor = Colors.Gray;
        Opacity = podeVer ? 1f : 0.2f;
    }
}

/// <summary>
/// ViewModel para a página de Manejo do Lote.
/// ✅ Atualiza automaticamente quando salva formulário
/// ✅ Suporta manejos personalizados
/// ✅ Sistema de abas: Geral / Por fase
/// </summary>
public partial class LoteManejoViewModel : ViewModelBase
{
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private Lote? lote;
    [ObservableProperty] private ObservableCollection<ManejoZootecnicoBox> manejoList = new();
    [ObservableProperty] private ObservableCollection<LoteForm> manejoGeralList = new(); // ★ NOVO: Lista geral
    
    // ★ NOVO: Lista de manejos personalizados (com propriedade manual para evitar bug do source generator)
    private ObservableCollection<ManejoZootecnicoBox> _manejoPersonalizadoList = new();
    public ObservableCollection<ManejoZootecnicoBox> ManejoPersonalizadoList
    {
        get => _manejoPersonalizadoList;
        set => SetProperty(ref _manejoPersonalizadoList, value);
    }

    // Controle de abas com enum
    private TabKind _tabSelecionada = TabKind.Geral;
    public TabKind TabSelecionada
    {
        get => _tabSelecionada;
        set
        {
            if (_tabSelecionada != value)
            {
                _tabSelecionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAbaGeralSelecionada));
                OnPropertyChanged(nameof(IsAbaPorIdadeSelecionada));
                OnPropertyChanged(nameof(ExibeAbaGeral));
                OnPropertyChanged(nameof(ExibeAbaPorIdade));
            }
        }
    }
    public bool IsAbaGeralSelecionada => TabSelecionada == TabKind.Geral;
    public bool IsAbaPorIdadeSelecionada => TabSelecionada == TabKind.PorIdade;
    public bool ExibeAbaGeral => IsAbaGeralSelecionada;
    public bool ExibeAbaPorIdade => IsAbaPorIdadeSelecionada;

    public LoteManejoViewModel()
    {
        // ✅ Mantém listener para atualizar caixas após salvar
        WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, async (r, m) => await OnFormularioSalvo(m.FormularioSalvo));
    }

    /// <summary>
    /// ✅ Chamado automaticamente quando salva formulário
    /// </summary>
    private async Task OnFormularioSalvo(LoteForm? loteForm)
    {
        if (loteForm?.loteId != Lote?.id) return;

        try
        {
            // ★ Recarrega aba Geral
            await CarregaManejoGeral();

            // Atualiza manejos padrão (Por fase)
            var caixa = ManejoList.FirstOrDefault(c => c.TipoParametro == loteForm.parametroTipoId && c.Fase == loteForm.loteFormFase);
            if (caixa != null)
            {
                await caixa.PegaDados(Lote, PodeEditarManejo, PodeVerManejo);
                var index = ManejoList.IndexOf(caixa);
                if (index >= 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ManejoList.RemoveAt(index);
                        ManejoList.Insert(index, caixa);
                    });
                }
            }

            // ★ NOVO: Atualiza manejos personalizados
            var caixaPersonalizada = ManejoPersonalizadoList.FirstOrDefault(c => 
                c.TipoParametro == loteForm.parametroTipoId && 
                c.LoteForm?.id == loteForm.id);
            
            if (caixaPersonalizada != null)
            {
                await caixaPersonalizada.PegaDados(Lote, PodeEditarManejo, PodeVerManejo);
                var index = ManejoPersonalizadoList.IndexOf(caixaPersonalizada);
                if (index >= 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ManejoPersonalizadoList.RemoveAt(index);
                        ManejoPersonalizadoList.Insert(index, caixaPersonalizada);
                    });
                }
            }
            else if (loteForm.parametroTipoId == 21) // Manejo Personalizado
            {
                // Adiciona novo manejo personalizado à lista
                await CarregaManejoPersonalizado();
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Erro ao atualizar: {ex.Message}"); }
    }

    /// <summary>
    /// Carrega os dados de manejo para o lote.
    /// </summary>
    public async Task CarregaDados(Lote lote)
    {
        if (lote == null) return;

        try
        {
            IsBusy = true;
            Title = Traducao.Manejo;
            Lote = lote;
            Lote.EnsureNames();
            ManejoList.Clear();
            ManejoPersonalizadoList.Clear();
            ManejoGeralList.Clear();

            // ★ Carrega lista GERAL (todas as datas de manejo)
            await CarregaManejoGeral();

            // ★ Manejos padrão (Por fase)
            var caixas = new[]
            {
                new ManejoZootecnicoBox { Numero = 1, TipoParametro = 4, DiasDe = -1000, DiasAte = 10, Nome = Traducao.Vazio, IsPersonalizado = false },
                new ManejoZootecnicoBox { Numero = 2, TipoParametro = 5, DiasDe = 1, DiasAte = 5, Nome = Traducao.Alojamento, IsPersonalizado = false },
                new ManejoZootecnicoBox { Numero = 3, TipoParametro = 6, DiasDe = 6, DiasAte = 14, Nome = Traducao.Inicial, IsPersonalizado = false },
                new ManejoZootecnicoBox { Numero = 4, TipoParametro = 7, DiasDe = 15, DiasAte = 21, Nome = Traducao.Crescimento1, IsPersonalizado = false },
                new ManejoZootecnicoBox { Numero = 5, TipoParametro = 8, DiasDe = 22, DiasAte = 35, Nome = Traducao.Crescimento2, IsPersonalizado = false },
                new ManejoZootecnicoBox { Numero = 6, TipoParametro = 9, DiasDe = 36, DiasAte = -9999, Nome = Traducao.Final, IsPersonalizado = false },
                new ManejoZootecnicoBox { Numero = 7, TipoParametro = 10, DiasDe = 1, DiasAte = 10, Nome = Traducao.Biosseguridade, IsPersonalizado = false }
            };

            foreach (var caixa in caixas)
            {
                await caixa.PegaDados(Lote, PodeEditarManejo, PodeVerManejo);
                caixa.ShowManejoCommand = new AsyncRelayCommand<ManejoZootecnicoBox>(ShowManejo); // ★ Adiciona comando
                ManejoList.Add(caixa);
            }

            // ★ NOVO: Carrega manejos personalizados
            await CarregaManejoPersonalizado();
        }
        catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    /// <summary>
    /// ★ NOVO: Carrega lista GERAL de manejos (aba "Geral")
    /// </summary>
    private async Task CarregaManejoGeral()
    {
        if (Lote == null) return;

        try
        {
            // Busca apenas os formulários de Manejo Geral (tipo 21) para este lote
            var tiposManejoQuery = new[] { 21 }; 
            var allForms = new List<LoteForm>();

            foreach (var tipo in tiposManejoQuery)
            {
                var forms = await LoteForm.PegaListaFormulariosLoteList((int)Lote.id, tipo, null);
                if (forms != null)
                    allForms.AddRange(forms);
            }

            // ★ OTIMIZAÇÃO: Popula cache do Lote UMA VEZ (antes de acessar IdadeLote)
            if (Lote?.id != null)
                _ = Lote.PegaLoteAsync((int)Lote.id); // Fire-and-forget

            // Ordena por data (mais recente primeiro)
            var formsOrdenados = allForms.OrderByDescending(f => f.data).ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ManejoGeralList.Clear();
                foreach (var form in formsOrdenados)
                    ManejoGeralList.Add(form);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar manejo geral: {ex.Message}");
        }
    }

    /// <summary>
    /// ★ NOVO: Carrega manejos personalizados (tipo 21 - Manejo Personalizado)
    /// </summary>
    private async Task CarregaManejoPersonalizado()
    {
        if (Lote == null) return;

        try
        {
            // Busca todos os formulários de Manejo Personalizado (tipo 21) para este lote
            var formsPersonalizados = await LoteForm.PegaListaFormulariosLoteList((int)Lote.id, 21, null);

            if (formsPersonalizados == null || formsPersonalizados.Count == 0)
                return;

            await MainThread.InvokeOnMainThreadAsync(() => ManejoPersonalizadoList.Clear());

            foreach (var form in formsPersonalizados)
            {
                var caixa = new ManejoZootecnicoBox
                {
                    TipoParametro = 21,
                    Nome = "Manejo Personalizado",
                    DiasDe = 1,
                    DiasAte = 10,
                    IsPersonalizado = true,
                    LoteForm = form,
                    ShowManejoCommand = new AsyncRelayCommand<ManejoZootecnicoBox>(ShowManejo)
                };

                await caixa.PegaDados(Lote, PodeEditarManejo, PodeVerManejo);
                await MainThread.InvokeOnMainThreadAsync(() => ManejoPersonalizadoList.Add(caixa));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar manejos personalizados: {ex.Message}");
        }
    }

    /// <summary>
    /// ★ NOVO: Adiciona manejo personalizado (tipo 21)
    /// </summary>
    [RelayCommand]
    public async Task AdicionarManejoPersonalizado()
    {
        if (!PodeEditarManejo || Lote == null) return;

        try
        {
            // Abre formulário de Manejo Personalizado (tipo 21) em branco
            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: -1,
                parametroTipoId: 21, // Manejo Personalizado
                fase: null,
                isReadOnly: false,
                podeEditar: true,
                item: null,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao adicionar manejo personalizado: {ex.Message}");
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro: {ex.Message}");
        }
    }

    /// <summary>
    /// ★ NOVO: Abre manejo da lista geral
    /// </summary>
    [RelayCommand]
    public async Task ShowManejoGeral(LoteForm? loteForm)
    {
        if (!PodeVerManejo || loteForm == null || Lote == null) return;

        await NavigationUtils.OpenLoteFormularioAsync(
            lote: Lote,
            loteFormId: loteForm.id ?? -1,
            parametroTipoId: loteForm.parametroTipoId ?? 0,
            fase: loteForm.loteFormFase,
            isReadOnly: true,
            podeEditar: PodeEditarManejo,
            item: loteForm.item,
            modeloIsiMacroSelecionado: null,
            limpaFormularioAtual: false);
    }

    [RelayCommand]
    public async Task ShowManejo(ManejoZootecnicoBox? manejo)
    {
        if (!PodeVerManejo || manejo == null || Lote == null) return;
        await NavigationUtils.OpenLoteFormularioAsync(
            lote: Lote,
            loteFormId: manejo.LoteForm?.id ?? -1,
            parametroTipoId: manejo.TipoParametro,
            fase: manejo.Fase,
            isReadOnly: manejo.LoteForm != null,
            podeEditar: PodeEditarManejo,
            item: manejo.LoteForm?.item,
            modeloIsiMacroSelecionado: null,
            limpaFormularioAtual: manejo.LoteForm == null);
    }

    /// <summary>
    /// ★ NOVO: Alterna entre abas
    /// </summary>
    [RelayCommand]
    public void SelecionarAba(object? param)
    {
        TabKind aba;
        if (param is TabKind tk)
        {
            aba = tk;
        }
        else if (param is string s && Enum.TryParse<TabKind>(s, true, out var parsed))
        {
            aba = parsed;
        }
        else
        {
            return;
        }
        TabSelecionada = aba;
    }

    public bool PodeVerManejo => Permissoes.PodeVerManejo;
    public bool PodeEditarManejo => Permissoes.UsuarioPermissoes?.lotes.monitoramento.manejo.editar ?? false;

    // ✅ Cleanup - Desregistra mensagens
    public override void Cleanup()
    {
        base.Cleanup();
        WeakReferenceMessenger.Default.Unregister<FormularioSalvoMessage>(this);
    }
}
