using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using ISIInstitute.Views;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utils;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SilvaData.ViewModels
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    public partial class ISIMacroButton : ObservableObject
    {
        [ObservableProperty] private int? numero;
        [ObservableProperty] private double score;
        [ObservableProperty] private DateTime data;
        [ObservableProperty] private LoteForm? loteForm;
        public Color SaudeColor => ISIMacro.StatusColor(Score);

        public IRelayCommand? ShowISIMacroCommand { get; set; }
    }

    public partial class LoteISIMacroViewModel : ViewModelBase
    {
        [ObservableProperty]
        string title;

        [ObservableProperty] private Lote? lote;
        [ObservableProperty] private ObservableCollection<ISIMacroButton> isiMacroList = new();
        [ObservableProperty] private List<LoteForm> isiMacroListForm = new();
        [ObservableProperty] private List<ModeloIsiMacroComParametros> modelosIsiMacro = new();
        [ObservableProperty] private ModeloIsiMacroComParametros? modeloIsiMacroSelecionado;

        public LoteISIMacroViewModel()
        {
            WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, async (r, m) => await OnFormularioSalvoAsync(m.FormularioSalvo));
            WeakReferenceMessenger.Default.Register<ISIMacroScoreMedioAtualizadoMessage>(this, (r, m) => OnISIMacroScoreMedioAtualizado(m.LoteId, m.NovoISIMacroScoreMedio));
            Title = Traducao.ISIMacro;
        }

        private async Task OnFormularioSalvoAsync(LoteForm? loteForm)
        {
            if (loteForm?.loteId != Lote?.id) return;
            if (loteForm?.parametroTipoId != 15) return;
            try
            {
                // Recarrega dados de ISI Macro para refletir novo score médio
                await CarregaDados(Lote!);
            }
            catch (Exception ex) { Debug.WriteLine($"Erro ao atualizar ISI Macro: {ex.Message}"); }
        }

        private void OnISIMacroScoreMedioAtualizado(int? loteId, double novoISIMacroScoreMedio)
        {
            if (loteId == null || Lote?.id == null || loteId != Lote.id) return;
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Lote != null)
                {
                    Lote.ISIMacroScoreMedio = novoISIMacroScoreMedio;
                    // Notifica que a propriedade Lote mudou para atualizar o binding
                    OnPropertyChanged(nameof(Lote));
                }
            });
        }

        public async Task CarregaDados(Lote? lote)
        {
            if (lote is null) return;
            try
            {
                // Set busy on UI thread to ensure bindings that depend on IsBusy update correctly
                await MainThread.InvokeOnMainThreadAsync(() => IsBusy = true);

                Lote = lote;
                var loteId = lote.id ?? throw new InvalidOperationException("Lote sem id para carregar ISI Macro.");
                lote.EnsureNames();

                NavigationUtils.LogExternal(nameof(LoteISIMacroViewModel), $"CarregaDados consultando formularios | lote={loteId}");
                var forms = await LoteForm.PegaListaFormulariosLoteList(loteId, 15, null);
                NavigationUtils.LogExternal(nameof(LoteISIMacroViewModel), $"CarregaDados formularios concluidos | forms={forms?.Count ?? 0}");

                NavigationUtils.LogExternal(nameof(LoteISIMacroViewModel), $"CarregaDados consultando scores | lote={loteId}");
                var scoresPorFormulario = await ISIMacro.GetScoresPorFormularioDoLoteAsync(loteId);
                NavigationUtils.LogExternal(nameof(LoteISIMacroViewModel), $"CarregaDados scores concluidos | scores={scoresPorFormulario.Count}");

                // assign backing lists (no UI thread required)
                IsiMacroListForm = forms ?? new List<LoteForm>();

                // Cria a lista fora da UI thread
                var tempList = new List<ISIMacroButton>();

                for (var index = IsiMacroListForm.Count - 1; index >= 0; index--)
                {
                    var form = IsiMacroListForm[index];
                    var score = form.id.HasValue && scoresPorFormulario.TryGetValue(form.id.Value, out var scoreFormulario)
                        ? scoreFormulario
                        : 0;

                    tempList.Add(new ISIMacroButton
                    {
                        Numero = form.item,
                        Score = score,
                        Data = form.data,
                        LoteForm = form,
                        ShowISIMacroCommand = new AsyncRelayCommand<ISIMacroButton>(ShowISIMacro)
                    });
                }

                NavigationUtils.LogExternal(nameof(LoteISIMacroViewModel), $"CarregaDados lista preparada | itens={tempList.Count}");

                // Atualiza a ObservableCollection de uma vez na UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // replace collection to ensure ListView picks up the change reliably
                    IsiMacroList = new ObservableCollection<ISIMacroButton>(tempList);

                    // Update Title based on actual items
                    Title = $"{Traducao.ISIMacro} ({IsiMacroList.Count})";

                    // Clear busy
                    IsBusy = false;
                });

                NavigationUtils.LogExternal(nameof(LoteISIMacroViewModel), $"CarregaDados UI atualizada | itens={tempList.Count}");
            }
            catch (Exception ex)
            {
                // Ensure IsBusy cleared and user notified on UI thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    IsBusy = false;
                    await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar ISI Macro: {ex.Message}");
                });
            }
        }

        public bool PodeVerISIMacro => Permissoes.UsuarioPermissoes?.lotes.monitoramento.isiMacro.consultar ?? false;
        public bool PodeEditarISIMacro => Permissoes.UsuarioPermissoes?.lotes.monitoramento.isiMacro.editar ?? false;


        public async Task ShowISIMacro(object isiMacro)
        {
            if (isiMacro is ISIMacroButton button)
                await ShowISIMacro(button);
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task ShowISIMacro(ISIMacroButton? isiMacro)
        {
            if (!PodeVerISIMacro || isiMacro?.LoteForm == null || Lote == null) 
                return;

            Debug.WriteLine($"[LoteISIMacroViewModel] 🚀 Abrindo ISIMacro - Ave {isiMacro.Numero}");

            // ★★★ OTIMIZAÇÃO: Navegação instantânea (dados carregam em background) ★★★
            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: isiMacro.LoteForm.id ?? -1,
                parametroTipoId: 15,
                fase: isiMacro.LoteForm.loteFormFase,
                isReadOnly: true,
                podeEditar: PodeEditarISIMacro,
                item: isiMacro.LoteForm.item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: false);

            Debug.WriteLine($"[LoteISIMacroViewModel] ✅ Navegação concluída");
        }

        [RelayCommand]
        public async Task NovoISIMacro()
        {
            if (!PodeEditarISIMacro || Lote == null) return;
            try
            {
                var item = IsiMacroListForm.Count == 0 ? 1 : IsiMacroListForm.Max(t => t.item.GetValueOrDefault()) + 1;
                ModeloIsiMacroSelecionado = null;

                if (ModelosIsiMacro.Count == 0)
                {
                    NavigationUtils.LogExternal(nameof(LoteISIMacroViewModel), "NovoISIMacro carregando modelos sob demanda");
                    ModelosIsiMacro = await ModeloIsiMacro.PegaModelosIsiMacroComParametrosAsync();
                    NavigationUtils.LogExternal(nameof(LoteISIMacroViewModel), $"NovoISIMacro modelos carregados | total={ModelosIsiMacro.Count}");
                }

                if (ModelosIsiMacro.Count > 0)
                {
                    var popup = ServiceHelper.GetRequiredService<SelectModeloPopup>();
                    popup.UpdateModelos(ModelosIsiMacro);
                    var selectedModeloObj = await NavigationUtils.ShowPopupAsync<ModeloIsiMacroComParametros>(popup);
                    if (selectedModeloObj is not ModeloIsiMacroComParametros selectedModelo) return;
                    ModeloIsiMacroSelecionado = selectedModelo;

                    // iOS: aguarda a animação de dismiss do popup terminar antes de empurrar modal.
                    // PresentViewController não pode ser chamado enquanto DismissViewController ainda está animando.
                    if (DeviceInfo.Platform == DevicePlatform.iOS)
                        await Task.Delay(600);
                }
                await NavigationUtils.OpenLoteFormularioAsync(
                    lote: Lote,
                    loteFormId: -1,
                    parametroTipoId: 15,
                    fase: null,
                    isReadOnly: false,
                    podeEditar: true,
                    item: item,
                    modeloIsiMacroSelecionado: ModeloIsiMacroSelecionado?.Id,
                    limpaFormularioAtual: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao criar ISI Macro: {ex.Message}");

                //Report Exception pro Sentry
                SentryHelper.CaptureExceptionWithUser(ex);

                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao criar ISI Macro: {ex.Message}");
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            WeakReferenceMessenger.Default.Unregister<FormularioSalvoMessage>(this);
            WeakReferenceMessenger.Default.Unregister<ISIMacroScoreMedioAtualizadoMessage>(this);
        }
    }
}
