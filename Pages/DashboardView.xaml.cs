using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    public partial class DashboardView : ContentView
    {
        private DashboardViewModel? _viewModel;
        private int _currentTabIndex;


        private View[] TabContents => [tabContent0, tabContent1, tabContent2];
        private Label[] TabLabels => [tabLabel0, tabLabel1, tabLabel2];

        public DashboardView()
        {
            InitializeComponent();
            BindingContext = ServiceHelper.GetRequiredService<DashboardViewModel>();
        }

        protected override void OnBindingContextChanged()
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            base.OnBindingContextChanged();

            _viewModel = BindingContext as DashboardViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                SyncTabImmediate(_viewModel.TabIndexSelecionado);
            }
        }

        private void SyncTabImmediate(int index)
        {
            for (int i = 0; i < TabContents.Length; i++)
            {
                TabContents[i].IsVisible = i == index;
                TabContents[i].Opacity = i == index ? 1.0 : 0.0;
                TabLabels[i].Opacity = i == index ? 1.0 : 0.55;
            }
            _currentTabIndex = index;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DashboardViewModel.TabIndexSelecionado)) return;
            if (sender is not DashboardViewModel vm) return;
            MainThread.BeginInvokeOnMainThread(() => SwitchTabImmediate(vm.TabIndexSelecionado));
        }

        private void SwitchTabImmediate(int newIndex)
        {
            if (newIndex == _currentTabIndex) return;
            for (int i = 0; i < TabContents.Length; i++)
            {
                TabContents[i].IsVisible = i == newIndex;
                TabContents[i].Opacity = i == newIndex ? 1.0 : 0.0;
                TabLabels[i].Opacity = i == newIndex ? 1.0 : 0.55;
            }
            _currentTabIndex = newIndex;
        }
    }
}
