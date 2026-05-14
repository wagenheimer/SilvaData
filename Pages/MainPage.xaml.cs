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
            // Após a primeira inicialização bem-sucedida, OnAppearing dispara toda vez
            // que um modal fecha. Não re-inicializar nesse caso para evitar ANR.
            if (ViewModel.JaInicializou && !SilvaData.Controls.Login.AcabouDeLogar)
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] OnAppearingInternalAsync ignorado: já inicializado.");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] OnAppearingInternalAsync START");

                // Aguarda a Activity estar totalmente resumida antes de qualquer navegação modal.
                // Sem esse delay, PushModalAsync pode causar tela preta no Android.
                await Task.Delay(300);

                if (ViewModel.InitializeAppCommand.CanExecute(null))
                {
                    await ViewModel.InitializeAppCommand.ExecuteAsync(null);
                }

                await ViewModel.AtualizaTotalSincronizacaoPendente();

                System.Diagnostics.Debug.WriteLine("[MainPage] OnAppearingInternalAsync DONE");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Erro em OnAppearing: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void OnTabSelectionChanged(object sender, TabSelectionChangedEventArgs e)
        {
            try
            {
                ViewModel.SelectedIndex = (int)e.NewIndex;

                // Índice 0 = Mercados
                if (e.NewIndex == 0)
                {
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
