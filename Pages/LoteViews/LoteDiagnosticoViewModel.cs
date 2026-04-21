using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.Utils;
using SilvaData.Pages.PopUps;

namespace SilvaData.ViewModels;

/// <summary>
/// Representa um diagnóstico na lista.
/// </summary>
public partial class DiagnosticoButton : ObservableObject
{
    [ObservableProperty]
    private DateTime data;

    [ObservableProperty]
    private string? diagnostico;

    [ObservableProperty]
    private int totalTratamentos;

    [ObservableProperty]
    private LoteForm? loteForm;
}

/// <summary>
/// ViewModel para Diagnóstico.
/// MIGRADO PARA MAUI: ServiceHelper, NavigationUtils.
/// </summary>
public partial class LoteDiagnosticoViewModel : ViewModelBase
{
    [ObservableProperty]
    private Lote? lote;

    [ObservableProperty]
    private ObservableCollection<DiagnosticoButton> diagnosticoList = new();

    private List<LoteForm> DiagnosticoListForm = new();
    private List<LoteForm> TratamentosListForm = new();

    /// <summary>
    /// ✅ Construtor vazio
    /// </summary>
    public LoteDiagnosticoViewModel()
    {
        WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, async (r, m) => await OnFormularioSalvoAsync(m.FormularioSalvo));
    }

    private async Task OnFormularioSalvoAsync(LoteForm? loteForm)
    {
        if (Lote == null) return;
        if (Lote.id is not int loteId) return;
        if (loteForm == null) return;

        var ehMesmoLote = loteForm.loteId == loteId;
        var ehTratamentoVinculadoAoDiagnosticoAtual = loteForm.parametroTipoId == 14
            && DiagnosticoList.Any(d => d.LoteForm?.id == loteForm.loteFormVinculado);

        if (!ehMesmoLote && !ehTratamentoVinculadoAoDiagnosticoAtual) return;
        if (loteForm.parametroTipoId != 13 && loteForm.parametroTipoId != 14) return;

        try { await CarregaDados(Lote); }
        catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao atualizar diagnósticos: {ex.Message}"); }
    }

    /// <summary>
    /// Carrega dados do diagnóstico.
    /// </summary>
    public async Task CarregaDados(Lote lote)
    {
        if (lote == null) return;

        try
        {
            IsBusy = true;
            Lote = lote;
            Lote.EnsureNames();

            DiagnosticoList.Clear();

            DiagnosticoListForm = await LoteForm.PegaListaFormulariosLoteList((int)Lote.id, 13, null);
            TratamentosListForm = await LoteForm.PegaListaFormulariosLoteList((int)Lote.id, 14, null);

            foreach (var diagnostico in DiagnosticoListForm)
            {
                var descricaoDiagnostico = (await LoteForm.PegaDiagnosticoLoteForm(diagnostico.id))?.Diagnostico ?? string.Empty;
                var totalVinculados = await LoteForm.TotalVinculados(diagnostico.id);

                DiagnosticoList.Add(new DiagnosticoButton
                {
                    LoteForm = diagnostico,
                    Data = diagnostico.data,
                    Diagnostico = descricaoDiagnostico,
                    TotalTratamentos = totalVinculados
                });
            }
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar diagnósticos: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Permissões.
    /// </summary>
    public bool PodeVerDiagnostico => Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
    public bool PodeEditarDiagnostico => Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.editar ?? false;

    /// <summary>
    /// Comando para visualizar diagnóstico.
    /// </summary>
    [RelayCommand]
    public async Task ShowDiagnostico(DiagnosticoButton? diagnostico)
    {
        if (!PodeVerDiagnostico || diagnostico?.LoteForm == null || Lote == null) return;
        await NavigationUtils.OpenLoteFormularioAsync(
            lote: Lote,
            loteFormId: diagnostico.LoteForm.id ?? -1,
            parametroTipoId: 13,
            fase: diagnostico.LoteForm.loteFormFase,
            isReadOnly: true,
            podeEditar: PodeEditarDiagnostico,
            item: diagnostico.LoteForm.item,
            modeloIsiMacroSelecionado: null,
            limpaFormularioAtual: false);
    }

    /// <summary>
    /// Abre formulário para ver/editar diagnóstico.
    /// </summary>
    public async Task FormularioVerOuEditar(LoteForm loteForm, int parametroTipo, bool podeEditar)
    {
        try
        {
            IsBusy = true;
            await Task.Delay(TimeSpan.FromMilliseconds(250));

            // ✅ USA ServiceHelper
            var viewModel = ServiceHelper.GetRequiredService<LoteFormularioViewModel>();

            viewModel.SetInitialState(
                lote: Lote!,
                loteFormId: (int)loteForm.id!,
                parametroTipoId: parametroTipo,
                fase: loteForm.loteFormFase,
                isReadOnly: true,
                podeEditar: podeEditar
            );

            viewModel.Item = loteForm.item;

            await viewModel.GetItemOrCreateANew();

            IsBusy = false;

            await NavigationUtils.ShowViewAsModalAsync<LoteFormularioView>();
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao abrir formulário: {ex.Message}");
            IsBusy = false;
        }
    }

    /// <summary>
    /// Comando para criar novo diagnóstico.
    /// </summary>
    [RelayCommand]
    public async Task NovoDiagnostico()
    {
        if (!PodeEditarDiagnostico || Lote == null) return;

        try
        {
            IsBusy = true;

            var item = DiagnosticoListForm.Count == 0
                ? 1
                : DiagnosticoListForm.Max(t => t.item.GetValueOrDefault()) + 1;

            IsBusy = false;
            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: -1,
                parametroTipoId: 13,
                fase: null,
                isReadOnly: false,
                podeEditar: true,
                item: item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: true);
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao criar diagnóstico: {ex.Message}");
            IsBusy = false;
        }
    }

    /// <summary>
    /// Comando para adicionar tratamento vinculado.
    /// </summary>
    [RelayCommand]
    public async Task AdicionarTratamento(DiagnosticoButton? diagnostico)
    {
        if (!PodeEditarDiagnostico || diagnostico?.LoteForm == null || Lote == null) return;

        try
        {
            IsBusy = true;

            var item = TratamentosListForm.Count == 0
                ? 1
                : TratamentosListForm.Max(t => t.item.GetValueOrDefault()) + 1;

            IsBusy = false;
            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: -1,
                parametroTipoId: 14,
                fase: null,
                isReadOnly: false,
                podeEditar: true,
                item: item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: true,
                loteFormVinculado: (int)diagnostico.LoteForm.id!);
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao adicionar tratamento: {ex.Message}");
            IsBusy = false;
        }
    }

    /// <summary>
    /// Cleanup para desregistrar listeners.
    /// </summary>
    public override void Cleanup()
    {
        base.Cleanup();
        WeakReferenceMessenger.Default.Unregister<FormularioSalvoMessage>(this);
    }
}
