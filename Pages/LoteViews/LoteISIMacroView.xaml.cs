using SilvaData.Models;
using SilvaData.ViewModels;

using Microsoft.Maui;
using Microsoft.Maui.Controls;

using System.Diagnostics;

namespace SilvaData.Controls
{
    public partial class LoteISIMacroView : ContentPage
    {
        private readonly LoteISIMacroViewModel _viewModel;
        private readonly Stopwatch _constructionStopwatch;
        private Lote? _lote;
        private int? _loadedLoteId;
        private bool _pendingReload;

        public LoteISIMacroView()
        {
            _constructionStopwatch = Stopwatch.StartNew();
            NavigationUtils.LogExternal(nameof(LoteISIMacroView), "Construtor iniciado");

            var initializeStopwatch = Stopwatch.StartNew();
            InitializeComponent();
            initializeStopwatch.Stop();
            NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"InitializeComponent concluido em {initializeStopwatch.ElapsedMilliseconds}ms");

            _viewModel = ServiceHelper.GetRequiredService<LoteISIMacroViewModel>();
            BindingContext = _viewModel;

            // Evita competir com a animação de modal no iOS.
            this.Opacity = DeviceInfo.Platform == DevicePlatform.iOS ? 1 : 0;

            _constructionStopwatch.Stop();
            NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"Construtor concluido em {_constructionStopwatch.ElapsedMilliseconds}ms | platform={DeviceInfo.Platform}");
        }

        public void SetInitialState(Lote lote)
        {
            ArgumentNullException.ThrowIfNull(lote);

            _lote = lote;
            _pendingReload = true;
            _loadedLoteId = null;

            NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"SetInitialState | lote={lote.id}");
        }

        private bool _isFirstAppearance = true;

        /// <summary>
        /// Carrega dados quando aparece.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = OnAppearingInternalAsync();
        }

        private async Task OnAppearingInternalAsync()
        {
            var appearingStopwatch = Stopwatch.StartNew();
            try
            {
                NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"OnAppearingInternalAsync iniciado | firstAppearance={_isFirstAppearance} | pendingReload={_pendingReload} | lote={_lote?.id}");

                if (DeviceInfo.Platform != DevicePlatform.iOS)
                {
                    await this.FadeToAsync(1, 300, Easing.CubicInOut);
                }

                if (_lote == null)
                {
                    NavigationUtils.LogExternal(nameof(LoteISIMacroView), "OnAppearingInternalAsync sem lote definido - ignorando carga");
                    return;
                }

                // Carrega na primeira vez ou quando uma nova abertura pediu recarga explícita.
                if (_isFirstAppearance || _pendingReload || _loadedLoteId != _lote.id)
                {
                    _isFirstAppearance = false;
                    _pendingReload = false;
                    _loadedLoteId = _lote.id;
                    NavigationUtils.LogExternal(nameof(LoteISIMacroView), "Cedendo frame antes de iniciar CarregaDados");
                    await Task.Yield();
                    NavigationUtils.LogExternal(nameof(LoteISIMacroView), "CarregaDados iniciando");
                    await _viewModel.CarregaDados(_lote);
                    NavigationUtils.LogExternal(nameof(LoteISIMacroView), "CarregaDados concluido");
                }
            }
            catch (Exception ex)
            {
                NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"Erro em OnAppearing: {ex.Message}");
            }
            finally
            {
                appearingStopwatch.Stop();
                NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"OnAppearingInternalAsync concluido em {appearingStopwatch.ElapsedMilliseconds}ms | firstAppearance={_isFirstAppearance} | pendingReload={_pendingReload} | lote={_lote?.id}");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            _ = NavigationUtils.PopModalAsync();
            return true;
        }
    }
}
