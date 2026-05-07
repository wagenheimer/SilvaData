using SilvaData.ViewModels;

using Microsoft.Maui.Controls;

namespace SilvaData.Pages
{
    public partial class SincronizacaoPageModal : ContentPage
    {
        private SincronizacaoViewModel? ViewModel => BindingContext as SincronizacaoViewModel;
        // Flag para disparar a sincronização uma única vez — OnAppearing pode ser
        // chamado mais de uma vez no iOS (ex: ao fechar um popup sobre esta página).
        private bool _hasStarted = false;

        public SincronizacaoPageModal(SincronizacaoViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            SincronizacaoViewControl.BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel != null && !_hasStarted && ViewModel.IniciaSincronizacaoCommand.CanExecute(null))
            {
                _hasStarted = true;
                _ = ViewModel.IniciaSincronizacaoCommand.ExecuteAsync(null);
            }
        }
    }
}
