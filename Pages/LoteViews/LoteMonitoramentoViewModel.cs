using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using ISIInstitute.Views.LoteViews;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;

using System.Diagnostics;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para monitoramento do lote (migrado de Xamarin.Forms para MAUI).
    /// </summary>
    public partial class LoteMonitoramentoViewModel : ViewModelBase
    {
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private Lote? loteAtual;

        [ObservableProperty]
        public bool sanidadeVisible = false;

        [ObservableProperty]
        public string title = string.Empty;

        [ObservableProperty]
        public string monitoramentoLoteText = string.Empty;

        [ObservableProperty]
        bool podeFecharLote;
        
        [ObservableProperty] private bool isManejoVisible = false;
        [ObservableProperty] private bool isZootecnicoVisible = false;
        [ObservableProperty] private bool isIsiMacroVisible = false;
        [ObservableProperty] private bool isNutricaoVisible = false;
        [ObservableProperty] private bool isSanidadeVisible = false;
        [ObservableProperty] private bool isIsiMicroVisible = false;
        [ObservableProperty] private bool isAvaliacaoGalpaoVisible = false;
        [ObservableProperty] private bool isEditarLoteVisible = false;

        // Permissões para sub-módulos de Sanidade
        [ObservableProperty] private bool isDiagnosticoVisible = false;
        [ObservableProperty] private bool isTratamentoVisible = false;
        [ObservableProperty] private bool isSalmonellaVisible = false;
        [ObservableProperty] private bool isVacinasVisible = false;

        public LoteMonitoramentoViewModel(CacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

            Title = Permissoes.TratamentoEmVezDeLote
                ? Traducao.InformaçõesDoTratamento
                : Traducao.InformaçõesDoLote;

            MonitoramentoLoteText = Permissoes.TratamentoEmVezDeLote
                ? Traducao.MonitoramentoDoTratamento
                : Traducao.MonitoramentoDoLote;

            WeakReferenceMessenger.Default.Register<LoteAlteradoMessage>(this, (sender, message) =>
            {
                message.Lote.EnsureNames(_cacheService);
                LoteAtual = message.Lote;
                AtualizaPodeFecharLote();
            });

            WeakReferenceMessenger.Default.Register<ISIMacroScoreMedioAtualizadoMessage>(this, (sender, message) =>
            {
                if (message.LoteId == null || LoteAtual == null || LoteAtual.id == null || LoteAtual.id != message.LoteId) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (LoteAtual == null) return;

                    LoteAtual.ISIMacroScoreMedio = message.NovoISIMacroScoreMedio;
                    OnPropertyChanged(nameof(LoteAtual));
                });
            });

            // Registrar listener para mudanças nas permissões
            Permissoes.StaticPropertyChanged += OnPermissoesChanged;

            AtualizaVisibilidadeModulos();
        }

        private void OnPermissoesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Atualiza visibilidade dos módulos quando qualquer permissão mudar
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AtualizaVisibilidadeModulos();
            });
        }

        private void AtualizaVisibilidadeModulos()
        {
            IsManejoVisible = Permissoes.PodeVerManejo;
            IsZootecnicoVisible = Permissoes.PodeVerZootecnico;
            IsIsiMacroVisible = Permissoes.PodeVerIsiMacro;
            IsNutricaoVisible = Permissoes.PodeVerNutricao;
            IsSanidadeVisible = Permissoes.PodeVerSanidade;
            if (!IsSanidadeVisible)
                SanidadeVisible = false;
            IsIsiMicroVisible = Permissoes.PodeVerIsiMicro;
            IsAvaliacaoGalpaoVisible = Permissoes.PodeVerAvaliacaoGalpao; // Agora usa galpao.consultar corretamente
            IsEditarLoteVisible = Permissoes.UsuarioPermissoes?.lotes.atualizar ?? false;
            
            // Permissões para sub-módulos de Sanidade
            IsDiagnosticoVisible = Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
            IsTratamentoVisible = Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
            IsSalmonellaVisible = Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
            IsVacinasVisible = Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
            
#if DEBUG
            Debug.WriteLine($"[LoteMonitoramento] Atualizando visibilidade:");
            Debug.WriteLine($"  - Manejo: {IsManejoVisible} (PodeVerManejo={Permissoes.PodeVerManejo})");
            Debug.WriteLine($"  - Zootecnico: {IsZootecnicoVisible} (PodeVerZootecnico={Permissoes.PodeVerZootecnico})");
            Debug.WriteLine($"  - ISIMacro: {IsIsiMacroVisible} (PodeVerIsiMacro={Permissoes.PodeVerIsiMacro})");
            Debug.WriteLine($"  - Nutricao: {IsNutricaoVisible} (PodeVerNutricao={Permissoes.PodeVerNutricao})");
            Debug.WriteLine($"  - Sanidade: {IsSanidadeVisible} (PodeVerSanidade={Permissoes.PodeVerSanidade})");
            Debug.WriteLine($"  - ISIMicro: {IsIsiMicroVisible} (PodeVerIsiMicro={Permissoes.PodeVerIsiMicro})");
            Debug.WriteLine($"  - AvaliacaoGalpao: {IsAvaliacaoGalpaoVisible} (galpao.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.galpao.consultar})");
            Debug.WriteLine($"  - Diagnostico: {IsDiagnosticoVisible} (sanidade.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar})");
            Debug.WriteLine($"  - Tratamento: {IsTratamentoVisible} (sanidade.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar})");
            Debug.WriteLine($"  - Salmonella: {IsSalmonellaVisible} (sanidade.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar})");
            Debug.WriteLine($"  - Vacinas: {IsVacinasVisible} (sanidade.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar})");
#endif
        }

        /// <summary>
        /// PADRÃO MAUI: Define o estado inicial com o Lote.
        /// </summary>
        public void SetInitialState(Lote lote)
        {
            if (lote == null)
                throw new ArgumentNullException(nameof(lote));

            // Garante preenchimento de nomes caso não tenham vindo via JOIN (fallback)
            lote.EnsureNames(_cacheService);
            LoteAtual = lote;
            AtualizaPodeFecharLote();
        }

        public async Task GetItemOrCreateANew()
        {
            AtualizaPodeFecharLote();
            await Task.CompletedTask;
        }

        [RelayCommand]
        public void ShowHideSanidade()
        {
            SanidadeVisible = !SanidadeVisible;
        }

        [RelayCommand]
        public async Task EditarLote()
        {
            if (LoteAtual == null) return;

            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await NavigationUtils.ShowViewAsModalAsync<LoteEditView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"{Traducao.ErroAoAbrir}: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task FecharLote()
        {
            if (LoteAtual == null) return;

            try
            {
                var confirmacao = await PopUpYesNo.ShowAsync(
                    Traducao.FecharLote,
                    Traducao.ConfirmeOFechamentoDolote);

                if (!confirmacao) return;

                IsBusy = true;

                LoteAtual.dataAbate = DateTime.Now;
                LoteAtual.conversaoAlimentarReal ??= 0;
                LoteAtual.mortalidade ??= 0;
                LoteAtual.pesoFinal ??= 0;
                LoteAtual.loteStatus = 2;

                await Lote.SaveLote(LoteAtual);
                AtualizaPodeFecharLote();

                WeakReferenceMessenger.Default.Send(new LoteAlteradoMessage(LoteAtual));

                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await PopUpOK.ShowAsync(Traducao.Sucesso, Traducao.OLote0FoiFechado);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"{Traducao.ErroAoFecharLote}: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ISIMacro()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                var isiMacroView = ServiceHelper.GetRequiredService<LoteISIMacroView>();
                isiMacroView.SetInitialState(LoteAtual);

                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    await NavigationUtils.ShowPageAsModalAsync(isiMacroView, animated: false);
                }
                else
                {
                    await NavigationUtils.ShowPageAsModalAsync(isiMacroView);
                }

                await RefreshLoteAtualAsync();
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task ISIMicro()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteISIMicroView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Manejo()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteManejoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Zootecnico()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteZootecnicoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Nutricao()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteNutricaoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Salmonella()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteSalmonellaView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Vacinas()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteVacinasView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Diagnostico()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteDiagnosticoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Tratamento()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteTratamentoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task AvaliacoesGalpao()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteAvaliacaoGalpaoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        private void AtualizaPodeFecharLote()
        {
            PodeFecharLote = (Permissoes.UsuarioPermissoes?.lotes.fechar ?? false)
                && (LoteAtual != null && LoteAtual.EstaFechado == false);
        }

        private async Task RefreshLoteAtualAsync()
        {
            if (LoteAtual?.id is not int loteId) return;

            try
            {
                var loteAtualizado = await Lote.PegaLoteAsync(loteId, forceRefresh: true);
                if (loteAtualizado == null) return;

                loteAtualizado.EnsureNames(_cacheService);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LoteAtual = loteAtualizado;
                    AtualizaPodeFecharLote();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteMonitoramento] Erro ao recarregar lote após ISIMacro: {ex.Message}");
            }
        }

        ~LoteMonitoramentoViewModel()
        {
            WeakReferenceMessenger.Default.Unregister<ISIMacroScoreMedioAtualizadoMessage>(this);
            WeakReferenceMessenger.Default.Unregister<LoteAlteradoMessage>(this);
            
            // Remover listener de permissões para evitar memory leak
            Permissoes.StaticPropertyChanged -= OnPermissoesChanged;
        }
    }
}
