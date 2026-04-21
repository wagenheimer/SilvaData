using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;


using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utils;
using SilvaData.Utilities;
using HapticHelper = global::SilvaData.Utilities.HapticHelper;

using System.Collections.ObjectModel;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a página de edição ou criação de uma Atividade.
    /// MIGRADO: Usa CacheService e Messaging (sem acoplamento à View).
    /// </summary>
    public partial class AtividadeEditViewModel : BaseEditViewModel
    {
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private Atividade atividade = new();

        [ObservableProperty]
        private UnidadeEpidemiologicaComDetalhes? selectedUE;

        [ObservableProperty]
        private bool mostraBotoesExtrasAtividate;

        [ObservableProperty]
        private bool estaInserindo;

        public List<string> NotificacaoDias { get; }
        public List<string> AtividadeNotificacaoTipo { get; }

        private int _atividadeId;

        /// <summary>
        /// MIGRADO: Construtor agora recebe CacheService via DI
        /// </summary>
        public AtividadeEditViewModel(CacheService cacheService)
        {
            _cacheService = cacheService;

            NotificacaoDias = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
            AtividadeNotificacaoTipo = new List<string> { Traducao.Horas, Traducao.Dias };
        }

        /// <summary>
        /// MIGRADO: Não recebe mais IValidatablePage (a View se registra sozinha via Messenger)
        /// </summary>
        public void SetInitialState(int id)
        {
            _hasLoaded = false;
            _atividadeId = id;
            EstaInserindo = (id == -1);
            MostraBotoesExtrasAtividate = false;
            if (id == -1)
            {
                SelectedUE = null;
            }
        }

        /// <summary>
        /// Carrega os dados da Atividade (se estiver editando) ou cria uma nova.
        /// </summary>
        public override async Task GetItemOrCreateANew()
        {
            Title = (_atividadeId != -1) ? Traducao.Atividade : Traducao.NovaAtividade;

            if (_atividadeId != -1)
            {
                Atividade = await Atividade.PegaAtividade(_atividadeId);
                IsReadOnly = true;
            }
            else
            {
                Atividade = new Atividade
                {
                    atividadeStatus = 1,
                    atividadeTipoData = 1,
                    dataInicio = DateTime.Today,
                    dataPrazo = DateTime.Today,
                    horaInicio = DateTime.Now.TimeOfDay,
                    horaPrazo = DateTime.Now.TimeOfDay,
                    Notificacoes = new ObservableCollection<AtividadeNotificacao>()
                };

                Atividade.Notificacoes.Add(new AtividadeNotificacao { Qtde = 1, Tipo = Traducao.Horas });
                IsReadOnly = false;
            }

            if (Atividade.unidadeEpidemiologicaId != null)
            {
                // MIGRADO: Usa CacheService ao invés de DadosStatic
                SelectedUE = _cacheService.UEList.FirstOrDefault(uep => uep.id == Atividade.unidadeEpidemiologicaId);
            }
        }

        public override void Edit()
        {
            base.Edit();
            MostraBotoesExtrasAtividate = true;
        }

        /// <summary>
        /// MIGRADO: Usa ValidateViewAsync() (Messaging) ao invés de View.ValidateFormAsync()
        /// </summary>
        public override async Task SaveAndReturn()
        {
            if (IsBusy) return;

            // 1. Aciona a validação da UI via Messenger
            if (!await ValidateViewAsync())
            {
                return;
            }

            // 2. Validação do ViewModel (ComboBox)
            if (string.IsNullOrWhiteSpace(Atividade.titulo) || SelectedUE == null)
            {
                await Helpers.ShowRequiredFieldsPopupAsync(new[]
                {
                    string.IsNullOrWhiteSpace(Atividade.titulo) ? Traducao.Título : null,
                    SelectedUE == null ? Traducao.UnidadeEpidemiológica : null
                });
                return;
            }

            // 3. Validação passou, continua
            try
            {
                IsBusy = true;
                DataSaved = true;

                Atividade.id = _atividadeId == -1 ? 0 : _atividadeId;
                Atividade.excluido = 0;
                Atividade.unidadeEpidemiologicaId = SelectedUE.id;

                await Atividade.SalvaAtividade(Atividade);

                if (EstaInserindo)
                {
                    foreach (var notificacao in Atividade.Notificacoes)
                    {
                        var prazoBase = Atividade.dataHoraPrazo ?? (Atividade.dataPrazo + Atividade.horaPrazo);

                        var novaNotificacao = new Notificacao
                        {
                            id = 0,
                            titulo = Atividade.titulo,
                            descricao = Atividade.descricao,
                            dataHora = notificacao.Tipo == Traducao.Horas
                                ? prazoBase.AddHours(-Convert.ToInt32(notificacao.Qtde))
                                : prazoBase.AddDays(-Convert.ToInt32(notificacao.Qtde))
                        };
                        await Notificacao.SalvaNotificacao(novaNotificacao);
                    }

                    HapticHelper.VibrateSuccess();
                    await PopUpOK.ShowAsync(Title, Traducao.AdicionadoComSucesso);
                    WeakReferenceMessenger.Default.Send(new AtividadeAdicionadaMessage(Atividade));
                }
                else
                {
                    HapticHelper.VibrateSuccess();
                    await PopUpOK.ShowAsync(Title, Traducao.EditadoComSucesso);
                    WeakReferenceMessenger.Default.Send(new AtividadeSalvaMessage(Atividade));
                }

                await Notificacao.CreateNotificationsAsync();

                // MUDANÇA: Usa ClosePageRequestMessage ao invés de BackNow()
                WeakReferenceMessenger.Default.Send(new ClosePageRequestMessage());
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

        /// <summary>
        /// Comando para finalizar a atividade (status = 2).
        /// </summary>
        [RelayCommand]
        private async Task FinalizarAtividade()
        {
            if (await PopUpYesNo.ShowAsync(
                Traducao.FinalizarAtividade,
                Traducao.DesejaMesmaFinalizarEstaAtividade,
                Traducao.Sim,
                Traducao.Não))
            {
                try
                {
                    IsBusy = true;
                    if (Atividade != null)
                    {
                        Atividade.atividadeStatus = 2;
                        await Atividade.SalvaAtividade(Atividade);

                        WeakReferenceMessenger.Default.Send(new AtividadeSalvaMessage(Atividade));
                    }

                    // MUDANÇA: Usa ClosePageRequestMessage
                    WeakReferenceMessenger.Default.Send(new ClosePageRequestMessage());
                }
                catch (Exception ex)
                {
                    await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao finalizar: {ex.Message}");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Comando para cancelar a atividade (status = 0).
        /// </summary>
        [RelayCommand]
        private async Task CancelarAtividade()
        {
            if (!await PopUpYesNo.ShowAsync(
                Traducao.CancelarAtividade,
                Traducao.CancelarAtividadeConfirmacao,
                Traducao.Sim,
                Traducao.Não))
                return;

            try
            {
                IsBusy = true;
                if (Atividade != null)
                {
                    Atividade.atividadeStatus = 0;
                    await Atividade.SalvaAtividade(Atividade);

                    WeakReferenceMessenger.Default.Send(new AtividadeSalvaMessage(Atividade));
                }

                // MUDANÇA: Usa ClosePageRequestMessage
                WeakReferenceMessenger.Default.Send(new ClosePageRequestMessage());
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao cancelar: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Adiciona um novo lembrete de notificação à lista.
        /// </summary>
        [RelayCommand]
        private void NovaNotificacao()
        {
            Atividade ??= new Atividade { Notificacoes = new ObservableCollection<AtividadeNotificacao>() };
            Atividade.Notificacoes.Add(new AtividadeNotificacao { Qtde = 1, Tipo = Traducao.Horas });
        }

        /// <summary>
        /// Remove um lembrete de notificação da lista.
        /// </summary>
        [RelayCommand]
        private void RemoveNotificacao(AtividadeNotificacao? notificacao)
        {
            if (notificacao != null && Atividade?.Notificacoes != null)
            {
                Atividade.Notificacoes.Remove(notificacao);
            }
        }
    }

    // --- MENSAGENS ---

    /// <summary>
    /// Enviada quando uma nova Atividade é adicionada.
    /// </summary>
    public class AtividadeAdicionadaMessage
    {
        public Atividade Atividade { get; }
        public AtividadeAdicionadaMessage(Atividade atividade) => Atividade = atividade;
    }

    /// <summary>
    /// Enviada quando uma Atividade existente é salva/atualizada.
    /// </summary>
    public class AtividadeSalvaMessage
    {
        public Atividade Atividade { get; }
        public AtividadeSalvaMessage(Atividade atividade) => Atividade = atividade;
    }
}
