using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using HapticHelper = global::SilvaData.Utilities.HapticHelper;


using System.Collections.ObjectModel;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// CORRIGIDO: ViewModel para edição de PROPRIEDADE (não Proprietário)
    /// </summary>
    public partial class PropriedadeEditViewModel : BaseEditViewModel
    {
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private Propriedade propriedade = new(); // CORRIGIDO: Era Proprietario

        [ObservableProperty]
        private List<Proprietario> proprietarios = new(); // Lista de proprietários para escolher

        [ObservableProperty]
        private ObservableCollection<ParametroComAlternativas> parametrosList = new();

        [ObservableProperty]
        private Proprietario? selectedProprietario;

        [ObservableProperty]
        private Regional? selectedRegional;

        // Listas do CacheService
        public ObservableCollection<Regional> Regionais => _cacheService.RegionalList;
        public ObservableCollection<Proprietario> ProprietariosDisponiveis => _cacheService.ProprietarioList;

        private int? _regionalId = -1;

        public PropriedadeEditViewModel(CacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Define o estado inicial do ViewModel.
        /// </summary>
        public void SetInitialState(Propriedade? propriedade, int? regionalID)
        {
            _hasLoaded = false;
            _regionalId = regionalID;

            if (propriedade == null)
            {
                NovoRegistro = true;
                Propriedade = new Propriedade { id = 0 };
                Title = Traducao.NovaPropriedade;
                SubTitle = Traducao.AdicioneAsInformaçõesAbaixo;
                SelectedProprietario = null;
                SelectedRegional = null;
            }
            else
            {
                NovoRegistro = false;
                Propriedade = propriedade;
                Title = Traducao.EditarPropriedade;
                SubTitle = Traducao.AdicioneAsInformaçõesAbaixo;
            }
        }

public override async Task GetItemOrCreateANew()
        {
            try
            {
                // Carrega proprietários disponíveis
                Proprietarios = await Proprietario.PegaListaProprietarios();

                // Carrega parâmetros
                var listaParams = await ParametroComAlternativas.LoteForm_PegaListaParametros(1);
                ParametrosList = new ObservableCollection<ParametroComAlternativas>(listaParams);

                // Se está editando, carrega selecoes
                if (!NovoRegistro)
                {
                    if (Propriedade?.regionalId != null)
                        SelectedRegional = Regionais?.FirstOrDefault(reg => reg?.id == Propriedade.regionalId);

                    if (Propriedade?.proprietarioId != null)
                        SelectedProprietario = Proprietarios?.FirstOrDefault(prop => prop?.id == Propriedade.proprietarioId);
                }
                else
                {
                    // Se for novo e tiver regional pré-selecionada
                    if (_regionalId != -1)
                    {
                        SelectedRegional = Regionais?.FirstOrDefault(r => r != null && r.id == _regionalId);
                        _regionalId = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropriedadeEditViewModel.GetItemOrCreateANew] Erro: {ex}");
            }
}

        /// <summary>
        /// CORRIGIDO: Sobrescreve com override (agora funciona porque AppearingBaseTask é virtual)
        /// </summary>
        public override async Task AppearingBaseTask()
        {
            if (HasLoaded) return; // CORRIGIDO: Usa HasLoaded (propriedade pública)
            DataSaved = false;
            IsBusy = true;
            try
            {
                // Sem delay - carrega direto
                await GetItemOrCreateANew();
                await CreateAdditionalFields();
                _hasLoaded = true; // CORRIGIDO: Acessa o protected field
                DataSaved = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public override async Task SaveAndReturn()
        {
            if (IsBusy) return;

            // Validação da UI
            if (!await ValidateViewAsync())
            {
                return;
            }

            // Validação do ViewModel
            if (SelectedProprietario == null || SelectedRegional == null)
            {
                await Helpers.ShowRequiredFieldsPopupAsync(new[]
                {
                    SelectedProprietario == null ? Traducao.Proprietário : null,
                    SelectedRegional == null ? Traducao.Regional : null
                });
                return;
            }

            try
            {
                IsBusy = true;
                DataSaved = true;

                Propriedade.excluido = 0;
                Propriedade.proprietarioId = SelectedProprietario.id;
                Propriedade.regionalId = SelectedRegional.id;

                await Propriedade.SalvaPropriedadeAsync(Propriedade);

                if (Propriedade.id != null)
                {
                    await Parametros.SalvaParametros(ParametrosList, "PropriedadeParametro", "propriedadeId", $"{Propriedade.id}");
                }

                string popupMessage;
                if (NovoRegistro)
                {
                    popupMessage = Traducao.PropriedadeAdicionadaComSucesso;
                    WeakReferenceMessenger.Default.Send(new PropriedadeAdicionadaMessage(Propriedade));
                }
                else
                {
                    popupMessage = Traducao.PropriedadeAlteradaComSucesso;
                    WeakReferenceMessenger.Default.Send(new PropriedadeSalvaMessage(Propriedade));
                }

                HapticHelper.VibrateClick();
                await PopUpOK.ShowAsync(Title, popupMessage);
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
    }
}
