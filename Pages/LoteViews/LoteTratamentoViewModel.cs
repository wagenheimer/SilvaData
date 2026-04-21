using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.Utils;
using SilvaData.Pages.PopUps;
using ISIInstitute.Views.LoteViews;

namespace SilvaData.ViewModels
{
    public partial class TratamentoButton : ObservableObject
    {
        [ObservableProperty] private DateTime data;
        [ObservableProperty] private LoteForm? loteForm;
    }

    public partial class LoteTratamentoViewModel : ViewModelBase
    {
        [ObservableProperty] private Lote? lote;
        [ObservableProperty] private ObservableCollection<TratamentoButton> tratamentosList = new();
        [ObservableProperty] private List<LoteForm> tratamentosListForm = new();

        public LoteTratamentoViewModel()
        {
            WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, async (r, m) => await OnFormularioSalvoAsync(m.FormularioSalvo));
        }

        private async Task OnFormularioSalvoAsync(LoteForm? loteForm)
        {
            if (loteForm?.loteId != Lote?.id) return;
            if (loteForm?.parametroTipoId != 14) return;
            try { await CarregaDados(Lote!); }
            catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao atualizar tratamentos: {ex.Message}"); }
        }

        public async Task CarregaDados(Lote lote)
        {
            if (lote == null) return;
            if (lote.id is not int loteId) return;

            try
            {
                IsBusy = true;
                Lote = lote;
                Lote.EnsureNames();
                TratamentosList.Clear();
                TratamentosListForm = await LoteForm.PegaListaFormulariosLoteList(loteId, 14, null);
                foreach (var tratamento in TratamentosListForm)
                {
                    TratamentosList.Add(new TratamentoButton { LoteForm = tratamento, Data = tratamento.data });
                }
            }
            catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar tratamentos: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        public bool PodeVerTratamentos => Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
        public bool PodeEditarTratamentos => Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.editar ?? false;

        [RelayCommand]
        public async Task ShowTratamentos(TratamentoButton? tratamento)
        {
            if (!PodeVerTratamentos || tratamento?.LoteForm == null || Lote == null) return;
            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: tratamento.LoteForm.id ?? -1,
                parametroTipoId: 14,
                fase: tratamento.LoteForm.loteFormFase,
                isReadOnly: true,
                podeEditar: PodeEditarTratamentos,
                item: tratamento.LoteForm.item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: false,
                loteFormVinculado: tratamento.LoteForm.loteFormVinculado ?? -1);
        }

        [RelayCommand]
        public async Task NovoTratamento()
        {
            if (!PodeEditarTratamentos || Lote == null) return;
            try
            {
                IsBusy = true;
                var item = TratamentosListForm.Count == 0 ? 1 : TratamentosListForm.Max(t => t.item.GetValueOrDefault()) + 1;
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
                    limpaFormularioAtual: true);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao criar tratamento: {ex.Message}");
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task Diagnostico()
        {
            if (Lote == null) return;
            try { await NavigationUtils.ShowViewAsModalAsync<LoteDiagnosticoView>(Lote); }
            catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro: {ex.Message}"); }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            WeakReferenceMessenger.Default.Unregister<FormularioSalvoMessage>(this);
        }
    }
}
