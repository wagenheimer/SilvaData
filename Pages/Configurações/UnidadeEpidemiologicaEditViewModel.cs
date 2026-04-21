using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Utils;
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using HapticHelper = global::SilvaData.Utilities.HapticHelper;


using System.Collections.ObjectModel;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a página de edição de Unidade Epidemiológica.
    /// MIGRADO: Usa CacheService e Messaging (sem acoplamento à View).
    /// </summary>
    public partial class UnidadeEpidemiologicaEditViewModel : BaseEditViewModel
    {
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private List<Proprietario> proprietarios = new();

        [ObservableProperty]
        private UnidadeEpidemiologica unidadeEpidemiologica = new();

        [ObservableProperty]
        private ObservableCollection<ParametroComAlternativas> unidadeEpidemiologica_ParametrosComAlternativas = new();

        [ObservableProperty]
        private Propriedade? selectedPropriedade;

        /// <summary>
        /// MIGRADO: Lista vem do CacheService injetado
        /// </summary>
        public ObservableCollection<Propriedade> Propriedades => _cacheService.PropriedadeList;

        // REMOVIDO: internal static Action<int?>? EventoAoAdicionar = null;
        // Agora usa WeakReferenceMessenger

        public int? RegionalId { get; set; } = -1;

        public UnidadeEpidemiologicaEditViewModel(CacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// MIGRADO: Não recebe mais IValidatablePage (a View se registra sozinha via Messenger)
        /// </summary>
        public void SetInitialState(UnidadeEpidemiologica? ue)
        {
            _hasLoaded = false;
            var isNovoRegistro = ue == null || ue.id <= 0;

            UnidadeEpidemiologica = ue ?? new UnidadeEpidemiologica { id = 0 };
            NovoRegistro = isNovoRegistro;

            if (isNovoRegistro)
            {
                Title = Traducao.NovaUnidadeEpidemiológica;
                SubTitle = Traducao.AdicioneAsInformaçõesAbaixo;
                SelectedPropriedade = null;
            }
            else
            {
                Title = Traducao.EditarUnidadeEpidemiológica;
                SubTitle = Traducao.AdicioneAsInformaçõesAbaixo;
            }
        }

        /// <summary>
        /// Carrega os dados assíncronos para a página.
        /// </summary>
        public override async Task GetItemOrCreateANew()
        {
            try
            {
                // Carrega proprietários disponíveis
                Proprietarios = await Proprietario.PegaListaProprietarios();

                // Garante que o CacheService está atualizado
                await _cacheService.UpdateRegionais();
                await _cacheService.UpdatePropriedade();

                // Verifica se a propriedade ainda existe na lista atualizada
                var propriedadeId = UnidadeEpidemiologica?.propriedadeId;
                var unidadeId = UnidadeEpidemiologica?.id ?? 0;

                if (!NovoRegistro)
                {
                    if (unidadeId > 0)
                    {
                        var ueAtualizada = await UnidadeEpidemiologica.GetItemAsync(unidadeId);
                        if (ueAtualizada != null)
                        {
                            UnidadeEpidemiologica = ueAtualizada;
                        }
                    }

                    var lista = (Permissoes.UsuarioPermissoes?.LoteDetalhado == true && unidadeId > 0)
                        ? await ParametroComAlternativas.UnidadeE_PegaParametrosComAlternativas(unidadeId)
                        : new List<ParametroComAlternativas>();

                    UnidadeEpidemiologica_ParametrosComAlternativas = new ObservableCollection<ParametroComAlternativas>(lista);

                    if (UnidadeEpidemiologica?.propriedadeId != null)
                    {
                        SelectedPropriedade = Propriedades?.FirstOrDefault(prop => prop?.id == UnidadeEpidemiologica.propriedadeId);
                    }
                }
                else
                {
                    _ = GetCurrentLocation();

                    if (propriedadeId.HasValue)
                    {
                        SelectedPropriedade = Propriedades?.FirstOrDefault(prop => prop?.id == propriedadeId.Value);
                    }

                    // Auto-seleciona se houver apenas 1 Propriedade disponível
                    if (SelectedPropriedade == null && Propriedades != null && Propriedades.Count == 1)
                        SelectedPropriedade = Propriedades[0];

                    var lista = (Permissoes.UsuarioPermissoes?.LoteDetalhado == true)
                        ? await ParametroComAlternativas.UnidadeE_PegaParametrosComAlternativas(0)
                        : new List<ParametroComAlternativas>();

                    UnidadeEpidemiologica_ParametrosComAlternativas = new ObservableCollection<ParametroComAlternativas>(lista);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UnidadeEpidemiologicaEditViewModel.GetItemOrCreateANew] Erro: {ex}");
            }
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
            if (SelectedPropriedade == null)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, "Selecione uma Propriedade antes de salvar.");
                return;
            }

            try
            {
                IsBusy = true;
                DataSaved = true;

                UnidadeEpidemiologica.excluido = 0;
                UnidadeEpidemiologica.propriedadeId = SelectedPropriedade.id;

                await UnidadeEpidemiologica.SalvaUEAsync(UnidadeEpidemiologica);

                await Parametros.SalvaParametros(
                    UnidadeEpidemiologica_ParametrosComAlternativas,
                    "UnidadeEpidemiologicaParametro",
                    "unidadeEpidemiologicaId",
                    $"{UnidadeEpidemiologica.id}"
                );

                string popupMessage;
                if (NovoRegistro)
                {
                    popupMessage = Traducao.UnidadeEpidemiológicaAdicionadaComSucesso;

                    // MUDANÇA: Usa Messaging ao invés de evento estático
                    WeakReferenceMessenger.Default.Send(new UEAdicionadaMessage(UnidadeEpidemiologica));

                    // REMOVIDO: await DadosStatic.instance.UpdateUnidadesEpidemiologicas();
                    // Isso já é feito automaticamente via RefreshCacheMessage em UnidadeEpidemiologica.SalvaUEAsync

                    // REMOVIDO: EventoAoAdicionar?.Invoke(UnidadeEpidemiologica.id);
                }
                else
                {
                    popupMessage = Traducao.UnidadeEpidemiológicaEditadaComSucesso;
                    WeakReferenceMessenger.Default.Send(new UESalvaMessage(UnidadeEpidemiologica));
                }

                await PopUpOK.ShowAsync(Title, popupMessage);
                SilvaData.Utilities.HapticHelper.VibrateClick();

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
        /// Tenta obter a geolocalização atual do dispositivo.
        /// Mostra mensagem ao usuário em caso de falha.
        /// </summary>
        private async Task GetCurrentLocation()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                    status = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.LocationWhenInUse>);

                if (status != PermissionStatus.Granted)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        PopUpOK.ShowAsync(Traducao.Aviso, "Permissão de localização negada. A latitude e longitude não serão preenchidas automaticamente."));
                    return;
                }

                var location = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

                if (location == null)
                    location = await Geolocation.GetLastKnownLocationAsync();

                if (location != null)
                {
                    UnidadeEpidemiologica.latitude = location.Latitude;
                    UnidadeEpidemiologica.longitude = location.Longitude;
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        PopUpOK.ShowAsync(Traducao.Aviso, "Não foi possível obter a localização (sem sinal de GPS). Preencha a latitude e longitude manualmente, se necessário."));
                }
            }
            catch (FeatureNotSupportedException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    PopUpOK.ShowAsync(Traducao.Aviso, "Este dispositivo não suporta GPS. A latitude e longitude não serão preenchidas automaticamente."));
            }
            catch (FeatureNotEnabledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    PopUpOK.ShowAsync(Traducao.Aviso, "O GPS está desligado no dispositivo. Ative a localização e tente novamente."));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro genérico ao obter Geolocation: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                    PopUpOK.ShowAsync(Traducao.Aviso, $"Erro ao obter localização: {ex.Message}"));
            }
        }
    }

}
