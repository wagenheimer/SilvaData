namespace SilvaData.Controls
{
    public partial class GalpoesView : ContentView
    {
        private GalpoesViewModel _vm;

        public GalpoesView()
        {
            InitializeComponent();
            _vm = ServiceHelper.GetRequiredService<GalpoesViewModel>();
            BindingContext = _vm;
        }

        private void OnShowLoteButtonTapped(object sender, TappedEventArgs e)
        {
            var grid = sender as Grid;
            var item = grid?.BindingContext as UnidadeEpidemiologicaComDetalhes;
            if (item != null && _vm.ShowLoteCommand.CanExecute(item))
                _vm.ShowLoteCommand.Execute(item);
        }

        private void OnEditarButtonTapped(object sender, TappedEventArgs e)
        {
            var grid = sender as Grid;
            var item = grid?.BindingContext as UnidadeEpidemiologicaComDetalhes;
            if (item != null && _vm.EditarCommand.CanExecute(item))
                _vm.EditarCommand.Execute(item);
        }
    }
}
