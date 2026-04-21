using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using SilvaData.Utils;

namespace SilvaData.ViewModels
{
    public partial class SalmonellaButton : ObservableObject
    {
        [ObservableProperty] private DateTime data;
        [ObservableProperty] private LoteForm? loteForm;
    }

    public partial class LoteSalmonellaViewModel : ViewModelBase
    {
        [ObservableProperty] private Lote? lote;
        [ObservableProperty] private ObservableCollection<SalmonellaButton> salmonellaList = new();
        [ObservableProperty] private List<LoteForm> salmonellaListForm = new();

        public LoteSalmonellaViewModel()
        {
            // Atualiza lista quando um formulário é salvo
            WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, async (r, m) => await OnFormularioSalvoAsync(m.FormularioSalvo));
        }

        private async Task OnFormularioSalvoAsync(LoteForm? loteForm)
        {
            if (loteForm?.loteId != Lote?.id) return;
            if (loteForm?.parametroTipoId != 18) return;
            try
            {
                // Atualiza ou adiciona
                var existente = SalmonellaList.FirstOrDefault(b => b.LoteForm?.id == loteForm.id);
                if (existente != null)
                {
                    existente.Data = loteForm.data;
                    existente.LoteForm = loteForm;
                }
                else
                {
                    SalmonellaList.Add(new SalmonellaButton { LoteForm = loteForm, Data = loteForm.data });
                }

                // Reordena (mais recente primeiro)
                var ordenado = SalmonellaList.OrderByDescending(b => b.Data).ToList();
                SalmonellaList.Clear();
                foreach (var b in ordenado) SalmonellaList.Add(b);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao atualizar lista de Salmonella: {ex.Message}");
            }
        }

        public async Task CarregaDados(Lote lote)
        {
            if (lote == null) return;
            try
            {
                IsBusy = true; Lote = lote; Lote.EnsureNames(); SalmonellaList.Clear();
                SalmonellaListForm = await LoteForm.PegaListaFormulariosLoteList((int)Lote.id, 18, null);
                foreach (var salmonella in SalmonellaListForm)
                    SalmonellaList.Add(new SalmonellaButton { LoteForm = salmonella, Data = salmonella.data });
            }
            catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar Salmonella: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        public bool PodeVerSalmonella => Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
        public bool PodeEditarSalmonella => Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.editar ?? false;

        [RelayCommand]
        public async Task ShowSalmonella(SalmonellaButton? salmonella)
        {
            if (!PodeVerSalmonella || salmonella?.LoteForm == null || Lote == null) return;
            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: salmonella.LoteForm.id ?? -1,
                parametroTipoId: 18,
                fase: salmonella.LoteForm.loteFormFase,
                isReadOnly: true,
                podeEditar: PodeEditarSalmonella,
                item: salmonella.LoteForm.item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: false);
        }

        [RelayCommand]
        public async Task NovaSalmonella()
        {
            if (!PodeEditarSalmonella || Lote == null) return;
            try
            {
                IsBusy = true;
                var item = SalmonellaListForm.Count == 0 ? 1 : SalmonellaListForm.Max(t => t.item.GetValueOrDefault()) + 1;
                IsBusy = false;
                await NavigationUtils.OpenLoteFormularioAsync(
                    lote: Lote,
                    loteFormId: -1,
                    parametroTipoId: 18,
                    fase: null,
                    isReadOnly: false,
                    podeEditar: true,
                    item: item,
                    modeloIsiMacroSelecionado: null,
                    limpaFormularioAtual: true);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao criar Salmonella: {ex.Message}");
                IsBusy = false;
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            WeakReferenceMessenger.Default.Unregister<FormularioSalvoMessage>(this);
        }
    }
}
