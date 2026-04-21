using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.Utils;
using SilvaData.Pages.PopUps;

namespace SilvaData.ViewModels
{
    public partial class VacinasButton : ObservableObject
    {
        [ObservableProperty] private DateTime data;
        [ObservableProperty] private LoteForm? loteForm;
    }

    public partial class LoteVacinasViewModel : ViewModelBase
    {
        [ObservableProperty] private Lote? lote;
        [ObservableProperty] private ObservableCollection<VacinasButton> vacinasList = new();
        [ObservableProperty] private List<LoteForm> vacinasListForm = new();

        public LoteVacinasViewModel()
        {
            WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, async (r, m) => await OnFormularioSalvoAsync(m.FormularioSalvo));
        }

        private async Task OnFormularioSalvoAsync(LoteForm? loteForm)
        {
            if (loteForm?.loteId != Lote?.id) return;
            if (loteForm?.parametroTipoId != 16) return;
            try { await CarregaDados(Lote!); }
            catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao atualizar vacinas: {ex.Message}"); }
        }

        public async Task CarregaDados(Lote lote)
        {
            if (lote == null) return;
            try
            {
                IsBusy = true;
                Lote = lote;
                Lote.EnsureNames();
                VacinasList.Clear();
                VacinasListForm = await LoteForm.PegaListaFormulariosLoteList((int)Lote.id, 16, null);
                foreach (var vacina in VacinasListForm)
                {
                    VacinasList.Add(new VacinasButton { LoteForm = vacina, Data = vacina.data });
                }
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar vacinas: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        public bool PodeVerVacinas => Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
        public bool PodeEditarVacinas => Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.editar ?? false;

        [RelayCommand]
        public async Task ShowVacinas(VacinasButton? vacina)
        {
            if (!PodeVerVacinas || vacina?.LoteForm == null || Lote == null) return;
            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: vacina.LoteForm.id ?? -1,
                parametroTipoId: 16,
                fase: vacina.LoteForm.loteFormFase,
                isReadOnly: true,
                podeEditar: PodeEditarVacinas,
                item: vacina.LoteForm.item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: false);
        }

        [RelayCommand]
        public async Task NovaVacina()
        {
            if (!PodeEditarVacinas || Lote == null) return;
            try
            {
                IsBusy = true;
                var item = VacinasListForm.Count == 0 ? 1 : VacinasListForm.Max(t => t.item.GetValueOrDefault()) + 1;
                IsBusy = false;
                await NavigationUtils.OpenLoteFormularioAsync(
                    lote: Lote,
                    loteFormId: -1,
                    parametroTipoId: 16,
                    fase: null,
                    isReadOnly: false,
                    podeEditar: true,
                    item: item,
                    modeloIsiMacroSelecionado: null,
                    limpaFormularioAtual: true);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao criar vacina: {ex.Message}");
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
