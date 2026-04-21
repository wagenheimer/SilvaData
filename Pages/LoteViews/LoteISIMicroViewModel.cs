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
    public partial class ISIMicroButton : ObservableObject
    {
        [ObservableProperty] private LoteForm? loteForm;
        [ObservableProperty] private DateTime data;
    }

    public partial class LoteISIMicroViewModel : ViewModelBase
    {
        [ObservableProperty] private Lote? lote;
        [ObservableProperty] private ObservableCollection<ISIMicroButton> isiMicroList = new();
        [ObservableProperty] private List<LoteForm> isiMicroListForm = new();

        public LoteISIMicroViewModel()
        {
            WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, async (r, m) => await OnFormularioSalvoAsync(m.FormularioSalvo));
        }

        private async Task OnFormularioSalvoAsync(LoteForm? loteForm)
        {
            if (loteForm?.loteId != Lote?.id) return;
            if (loteForm?.parametroTipoId != 17) return;
            try { await CarregaDados(Lote!); }
            catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao atualizar ISI Micro: {ex.Message}"); }
        }

        public async Task CarregaDados(Lote lote)
        {
            if (lote == null) return;
            try
            {
                IsBusy = true; Lote = lote; Lote.EnsureNames(); IsiMicroList.Clear();
                IsiMicroListForm = await LoteForm.PegaListaFormulariosLoteList((int)Lote.id, 17, null);
                foreach (var isiMicro in IsiMicroListForm)
                    IsiMicroList.Add(new ISIMicroButton { LoteForm = isiMicro, Data = isiMicro.data });
            }
            catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar ISI Micro: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        public bool PodeVerISIMicro => Permissoes.UsuarioPermissoes?.lotes.monitoramento.isiMicro.consultar ?? false;
        public bool PodeEditarISIMicro => Permissoes.UsuarioPermissoes?.lotes.monitoramento.isiMicro.editar ?? false;

        [RelayCommand]
        public async Task ShowISIMicro(ISIMicroButton? isiMicro)
        {
            if (!PodeVerISIMicro || isiMicro?.LoteForm == null || Lote == null) return;
            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: isiMicro.LoteForm.id ?? -1,
                parametroTipoId: 17,
                fase: isiMicro.LoteForm.loteFormFase,
                isReadOnly: true,
                podeEditar: PodeEditarISIMicro,
                item: isiMicro.LoteForm.item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: false);
        }

        [RelayCommand]
        public async Task NovoISIMicro()
        {
            if (!PodeEditarISIMicro || Lote == null) return;
            try
            {
                IsBusy = true;
                var item = IsiMicroListForm.Count == 0 ? 1 : IsiMicroListForm.Max(t => t.item.GetValueOrDefault()) + 1;
                IsBusy = false;
                await NavigationUtils.OpenLoteFormularioAsync(
                    lote: Lote,
                    loteFormId: -1,
                    parametroTipoId: 17,
                    fase: null,
                    isReadOnly: false,
                    podeEditar: true,
                    item: item,
                    modeloIsiMacroSelecionado: null,
                    limpaFormularioAtual: true);
            }
            catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao criar ISI Micro: {ex.Message}"); IsBusy = false; }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            WeakReferenceMessenger.Default.Unregister<FormularioSalvoMessage>(this);
        }
    }
}
