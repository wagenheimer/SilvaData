using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.PageModels;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Utilities;
using Syncfusion.Maui.Toolkit.TabView;

namespace SilvaData.Pages
{
    /// <summary>
    /// A View principal da aplicação (ContentPage).
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly MainPageModel ViewModel;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="MainPage"/>.
        /// </summary>
        public MainPage(MainPageModel mainPageModel)
        {
            InitializeComponent();

            ViewModel = mainPageModel;
            BindingContext = ViewModel;

            // Sincroniza SelectedIndex do ViewModel → SfTabView sem usar binding (evita conflito de state)
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainPageModel.SelectedIndex))
                    MainThread.BeginInvokeOnMainThread(() => mainTabView.SelectedIndex = ViewModel.SelectedIndex);
            };

            // Mantém a tela ativa
            DeviceDisplay.KeepScreenOn = true;
        }

        /// <summary>
        /// Chamado quando a página é exibida.
        /// Aciona o comando de inicialização do aplicativo no ViewModel.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = OnAppearingInternalAsync();
        }

        private async Task OnAppearingInternalAsync()
        {
            try
            {
                if (ViewModel.InitializeAppCommand.CanExecute(null))
                {
                    await ViewModel.InitializeAppCommand.ExecuteAsync(null);
                }

                await ViewModel.AtualizaTotalSincronizacaoPendente();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Erro em OnAppearing: {ex.Message}");
            }
        }

        private void OnTabSelectionChanged(object sender, TabSelectionChangedEventArgs e)
        {
            try
            {
                // No Toolkit 1.0.9, a propriedade ainda é NewIndex (retorna double)
                ViewModel.SelectedIndex = (int)e.NewIndex;

                // Índice 0 = Dashboard
                if (e.NewIndex == 0)
                {
                    // Envia mensagem para revalidar dados (dashboard decide debounce)
                    WeakReferenceMessenger.Default.Send(new ShowDashboardMessage());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro OnTabSelectionChanged: {ex.Message}");
            }
        }
    }
}
