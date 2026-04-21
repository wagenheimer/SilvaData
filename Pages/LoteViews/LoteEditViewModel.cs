using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Utils;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utils;
using SilvaData.Utilities;
using HapticHelper = SilvaData.Utilities.HapticHelper;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a página de edição ou criação de um Lote/Tratamento.
    /// MIGRADO: Usa CacheService ao invés de LoteService.
    /// Herda de <see cref="BaseEditViewModel"/>.
    /// </summary>
    public partial class LoteEditViewModel : BaseEditViewModel
    {
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private Lote lote = new();

        [ObservableProperty]
        private ObservableCollection<LoteVisita> loteVisitas = new();

        [ObservableProperty]
        private ObservableCollection<ParametroComAlternativas> lote_ParametrosComAlternativas = new();

        [ObservableProperty]
        private UnidadeEpidemiologicaComDetalhes? selectedUE;

        [ObservableProperty]
        private bool editando;

        /// <summary>
        /// MIGRADO: Obtém a lista de Unidades Epidemiológicas do CacheService.
        /// </summary>
        public ObservableCollection<UnidadeEpidemiologicaComDetalhes> UnidadesEpidemiologicas => _cacheService.UEList;

        /// <summary>
        /// MIGRADO: Construtor agora recebe CacheService via DI
        /// </summary>
        public LoteEditViewModel()
        {
            _cacheService = ServiceHelper.GetRequiredService<CacheService>();
        }

        /// <summary>
        /// Define o estado inicial do ViewModel com base no Lote fornecido.
        /// </summary>
        public void SetInitialState(Lote? loteAtual, bool retomarEmAndamento = false)
        {
            _hasLoaded = false;
            Lote = loteAtual ?? new Lote { id = 0, dataInicio = DateTime.Now };
            Lote.EnsureNames();
            Editando = (loteAtual != null && loteAtual.id > 0);
            
            if (!Editando)
            {
                SelectedUE = null;
            }
        }

        /// <summary>
        /// Carrega os dados assíncronos (parâmetros, visitas, etc.) para a página.
        /// </summary>
        public override async Task GetItemOrCreateANew()
        {
            if (Lote == null) return;

            Title = Permissoes.TratamentoEmVezDeLote
                ? (Editando ? Traducao.Tratamento : Traducao.NovoTratamento)
                : (Editando ? Traducao.Lote : Traducao.NovoLote);

            IsReadOnly = Editando;

            var parameters = await ParametroComAlternativas.Lote_PegaParametrosComAlternativas(Lote, !Permissoes.PermissaoLoteDetalhado);
            Lote_ParametrosComAlternativas = new ObservableCollection<ParametroComAlternativas>(parameters);

            if (Editando)
            {
                if (Lote.id > 0)
                {
                    // Recarrega o lote do banco para garantir dados atualizados
                    var loteDoBanco = await Lote.PegaLoteAsync((int)Lote.id);
                    if (loteDoBanco != null) 
                    {
                        Lote = loteDoBanco;
                        Lote.EnsureNames();
                    }

                    var visitas = await LoteVisita.PegaListaVisitasAsync((int)Lote.id);
                    LoteVisitas = new ObservableCollection<LoteVisita>(visitas);
                }

                if (Lote.unidadeEpidemiologicaId != null)
                {
                    SelectedUE = UnidadesEpidemiologicas.FirstOrDefault(ue => ue.id == Lote.unidadeEpidemiologicaId);
                }
            }
            else // Novo Lote
            {
                // Se um ID de UE foi passado (ex: vindo da página de UE)
                if (Lote.unidadeEpidemiologicaId > 0)
                {
                    SelectedUE = UnidadesEpidemiologicas.FirstOrDefault(ue => ue.id == Lote.unidadeEpidemiologicaId);
                }
                // Se só houver uma UE na lista filtrada, pré-seleciona
                else if (UnidadesEpidemiologicas.Count == 1)
                {
                    SelectedUE = UnidadesEpidemiologicas[0];
                }
            }

            AbrirLoteCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Salva o lote, parâmetros e navega para a tela anterior.
        /// </summary>
        public override async Task SaveAndReturn()
        {
            Debug.WriteLine($"[LoteEdit.SaveAndReturn] INÍCIO — IsBusy={IsBusy}, Thread={Environment.CurrentManagedThreadId}");

            if (IsBusy)
            {
                Debug.WriteLine("[LoteEdit.SaveAndReturn] BLOQUEADO — IsBusy=true, saindo");
                return;
            }

            // 1. Validação da View (DESACOPLADO VIA MESSAGING)
            Debug.WriteLine("[LoteEdit.SaveAndReturn] Chamando ValidateViewAsync...");
            if (!await ValidateViewAsync())
            {
                Debug.WriteLine($"[LoteEdit.SaveAndReturn] ValidateViewAsync FALHOU — Thread={Environment.CurrentManagedThreadId}, IsBusy={IsBusy}");
                Debug.WriteLine($"[LoteEdit.SaveAndReturn] SaveAndReturnCommand.CanExecute={SaveAndReturnCommand.CanExecute(null)}");
                return; // Falhou na validação da UI
            }

            // 2. Validação do ViewModel
            if (SelectedUE == null)
            {
                Debug.WriteLine("[LoteEdit.SaveAndReturn] SelectedUE é null — mostrando popup");
                await Helpers.ShowRequiredFieldsPopupAsync(new[] { Traducao.UnidadeEpidemiológica });
                Debug.WriteLine($"[LoteEdit.SaveAndReturn] Popup fechado — Thread={Environment.CurrentManagedThreadId}, IsBusy={IsBusy}, CanExecute={SaveAndReturnCommand.CanExecute(null)}");
                return;
            }

            if (string.IsNullOrWhiteSpace(Lote.numero))
            {
                Debug.WriteLine("[LoteEdit.SaveAndReturn] Lote.numero vazio — mostrando popup");
                await Helpers.ShowRequiredFieldsPopupAsync(new[]
                {
                    Permissoes.TratamentoEmVezDeLote ? Traducao.Tratamento : Traducao.Lote
                });
                Debug.WriteLine($"[LoteEdit.SaveAndReturn] Popup fechado — Thread={Environment.CurrentManagedThreadId}, IsBusy={IsBusy}, CanExecute={SaveAndReturnCommand.CanExecute(null)}");
                return;
            }

            // 3. Lógica de salvar
            IsBusy = true;

            try
            {
                Lote.excluido = 0;
                Lote.unidadeEpidemiologicaId = SelectedUE.id;
                Lote.UnidadeEpidemiologicaNome = SelectedUE.nome ?? string.Empty;
                Lote.PropriedadeNome = SelectedUE.PropriedadeNome ?? string.Empty;
                Lote.RegionalNome = SelectedUE.RegionalNome ?? string.Empty;
                Lote.mortalidade ??= 0;
                Lote.conversaoAlimentarReal ??= 0;
                Lote.pesoFinal ??= 0;
                Lote.pesoInicial ??= 0;

                await Lote.SaveLote(Lote);

                if (Lote.id > 0)
                {
                    await Parametros.SalvaParametros(Lote_ParametrosComAlternativas, "LoteParametro", "loteId", Lote.id.ToString());
                }

                DataSaved = true;

                string popupTitle = $"{Title} - {Lote.numero}";
                string popupMessage;

                if (!Editando)
                {
                    popupMessage = Traducao.AdicionadoComSucesso;
                    // ADICIONADO: Envia mensagem de novo lote
                    WeakReferenceMessenger.Default.Send(new NovoLoteMessage(Lote));
                }
                else
                {
                    popupMessage = Traducao.AsAlteraçõesForamSalvas;
                    // ADICIONADO: Envia mensagem de lote alterado
                    WeakReferenceMessenger.Default.Send(new LoteAlteradoMessage(Lote));
                }

                HapticHelper.VibrateSuccess();
                await PopUpOK.ShowAsync(popupTitle, popupMessage);

                // 4. Pede à View para fechar
                WeakReferenceMessenger.Default.Send(new ClosePageRequestMessage());
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao salvar lote: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Comando para "Abrir" um lote, resetando seus status e datas.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteAbrirLote))]
        private void AbrirLote()
        {
            if (Lote == null) return;

            Lote.dataAbate = DateTime.MinValue;
            Lote.mortalidade = 0;
            Lote.conversaoAlimentarReal = 0;
            Lote.pesoFinal = 0;
            Lote.loteStatus = 1; // Status 1 = Aberto

            // Notifica a UI que o objeto Lote mudou
            OnPropertyChanged(nameof(Lote));
            AbrirLoteCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Verifica se o usuário pode abrir um lote.
        /// </summary>
        private bool CanExecuteAbrirLote()
        {
            bool podeAbrir = PodeEditar && (Permissoes.UsuarioPermissoes?.lotes.abrir ?? false);
            return podeAbrir && Lote?.loteStatus != 1;
        }

        /// <summary>
        /// Verifica se o usuário pode editar lotes.
        /// </summary>
        public override bool PodeEditar => Permissoes.UsuarioPermissoes?.lotes.atualizar ?? false;
    }
}
