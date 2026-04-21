using SilvaData.Infrastructure;
using SilvaData.ViewModels;
using SilvaData.Utilities;
using Microsoft.Maui.Controls;

namespace SilvaData.Pages
{
    public class GraficoResultadosPage : ContentPage
    {
        private GraficoResultadosViewModel ViewModel => BindingContext as GraficoResultadosViewModel;

        public GraficoResultadosPage()
        {
            Title = "Resultados";
            BackgroundColor = ISIUtils.GetResourceColor("PrimaryColor", Colors.White);
            BindingContext = ServiceHelper.GetRequiredService<GraficoResultadosViewModel>();

            var view = new SilvaData.Controls.GraficoResultadosView();
            Content = new Grid
            {
                BackgroundColor = ISIUtils.GetResourceColor("QuaseBranco", Colors.White),
                RowDefinitions = new RowDefinitionCollection { new RowDefinition(GridLength.Star) },
                Children = { view }
            };
        }

        public GraficoResultadosPage(DashboardTipoGrafico tipo, string? superCategoria = null, string? categoria = null) : this()
        {
            if (superCategoria != null) ViewModel.SuperCategoriaSelecionada = superCategoria;
            if (categoria != null) ViewModel.CategoriaSelecionada = categoria;
            ViewModel.GetType().GetMethod("SetTipoGrafico", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(ViewModel, new object[] { tipo });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel?.AtualizaGraficos();
        }
    }
}
