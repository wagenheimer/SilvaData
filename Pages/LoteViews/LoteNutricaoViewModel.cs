using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Infrastructure;
using SilvaData.Models;
using ISIInstitute.Views.LoteViews;
using SilvaData.Utilities;
using SilvaData.Utils;
using SilvaData.Pages.PopUps;
using SilvaData.Controls;

namespace SilvaData.ViewModels
{
    public partial class NutricaoButton : ObservableObject
    {
        [ObservableProperty] private string? fase;
        [ObservableProperty] private LoteForm? loteForm;
    }

    public partial class LoteNutricaoViewModel : ViewModelBase
    {
        [ObservableProperty] private string title = string.Empty;
        [ObservableProperty] private Lote? lote;
        [ObservableProperty] private ObservableCollection<NutricaoButton> nutricaoList = new();
        [ObservableProperty] private List<LoteForm> nutricaoListForm = new();

        public LoteNutricaoViewModel()
        {
            WeakReferenceMessenger.Default.Register<CloseFormularioMessage>(this, async (r, m) =>
            {
                try
                {
                    if (Lote != null && m.MostraLoading)
                    {
                        Debug.WriteLine("[LoteNutricaoViewModel] 🔄 Formulário fechou - recarregando lista");
                        await CarregaDados(Lote);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LoteNutricaoViewModel] Erro ao recarregar: {ex.Message}");
                }
            });
        }

        public async Task CarregaDados(Lote lote)
        {
            if (lote == null) return;
            try
            {
                Debug.WriteLine($"[LoteNutricaoViewModel] 📥 Carregando nutrição do lote {lote.id}");
                IsBusy = true;
                Title = Traducao.Nutrição;
                Lote = lote;
                Lote.EnsureNames();
                NutricaoList.Clear();
                NutricaoListForm = await LoteForm.PegaListaFormulariosLoteList((int)Lote.id, 12, null);
                Debug.WriteLine($"[LoteNutricaoViewModel] ✅ Encontrados {NutricaoListForm.Count} registros");
                foreach (var nutricao in NutricaoListForm)
                {
                    var nutricaoDetail = await ParametroComAlternativas.LoteForm_PegaListaParametros(12, (int)nutricao.id!, -1, null);
                    var nutricaoFase = nutricaoDetail.FirstOrDefault(d => d.AlternativaSelecionada != null)?.AlternativaSelecionada?.descricao;
                    NutricaoList.Add(new NutricaoButton { LoteForm = nutricao, Fase = nutricaoFase });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteNutricaoViewModel] ❌ Erro: {ex.Message}");
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar nutrição: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        public bool PodeVerNutricao => Permissoes.UsuarioPermissoes?.lotes.monitoramento.nutricao.consultar ?? false;
        public bool PodeEditarNutricao => Permissoes.UsuarioPermissoes?.lotes.monitoramento.nutricao.editar ?? false;

        [RelayCommand]
        public async Task ShowNutricao(NutricaoButton? nutricao)
        {
            if (!PodeVerNutricao || nutricao?.LoteForm == null || Lote == null) return;
            await AbrirFormularioAsync(nutricao.LoteForm, true, PodeEditarNutricao, false);
        }

        [RelayCommand]
        public async Task NovaNutricao()
        {
            if (!PodeEditarNutricao || Lote == null) return;
            Debug.WriteLine("[LoteNutricaoViewModel] ➕ Criando nova nutrição");
            var item = NutricaoListForm.Count == 0 ? 1 : NutricaoListForm.Max(t => t.item.GetValueOrDefault()) + 1;
            await AbrirFormularioAsync(new LoteForm { id = -1, item = item }, false, true, true, item);
        }

        private async Task AbrirFormularioAsync(LoteForm loteForm, bool isReadOnly, bool podeEditar, bool novo, int? item = null)
        {
            if (Lote == null) return;
            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: loteForm.id ?? -1,
                parametroTipoId: 12,
                fase: loteForm.loteFormFase,
                isReadOnly: isReadOnly,
                podeEditar: podeEditar,
                item: item ?? loteForm.item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: novo);
        }
    }
}
