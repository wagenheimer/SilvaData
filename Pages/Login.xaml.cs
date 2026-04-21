using SilvaData.Infrastructure;
using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    /// <summary>
    /// View (Página) para Login. Esta página é modal e não pode ser fechada
    /// pelo usuário (ex: botão "Voltar" do Android).
    /// </summary>
    public partial class Login : ContentPage
    {
        /// <summary>
        /// Flag estática usada pelo MainPageModel para saber
        /// que o app deve rodar a sincronização inicial.
        /// </summary>
        public static bool AcabouDeLogar;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Login"/>.
        /// </summary>
        public Login(LoginViewModel viewModel)
        {
            InitializeComponent();

            Shell.SetNavBarIsVisible(this, false);

            // Define o BindingContext para o ViewModel injetado
            BindingContext = viewModel;
        }

        /// <summary>
        /// Chamado quando a página é exibida.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Define a flag estática que o MainPageModel usará
            AcabouDeLogar = true;

            // Animação de Fade-in (lógica de View)
            // Assumindo que o Grid no XAML tem x:Name="loginPanel"
            var loginPanel = this.FindByName<Grid>("loginPanel");
            if (loginPanel != null)
            {
                _ = loginPanel.FadeToAsync(1, 500);
            }
        }

        /// <summary>
        /// CORREÇÃO: Impede que o botão "Voltar" do hardware (Android)
        /// feche a página de login.
        /// </summary>
        /// <returns>Sempre <c>true</c> para indicar que o evento foi tratado.</returns>
        protected override bool OnBackButtonPressed()
        {
            // Retorna 'true' para "consumir" o evento e impedir
            // que a página seja fechada.
            return true;
        }
    }
}