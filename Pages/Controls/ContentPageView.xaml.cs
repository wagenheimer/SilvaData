

using Syncfusion.Maui.Core;
using Syncfusion.Maui.DataGrid;

namespace SilvaData.Controls
{
    public partial class ContentPageView : ContentPageWithLocalization
    {
        public SfBusyIndicator busyindicator;

        public bool CanReloadData = true;

        public View dados;

        public SfDataGrid ListView;


        public SearchBar SearchBar;

        public ContentPageView()
        {
            InitializeComponent();
        }

        public Command _VerDetalhesCommand { get; private set; }

        public Command VerDetalhesCommand => _VerDetalhesCommand ??= new Command(async () => await VerDetalhes(), () => TemItemSelecionado);

        public Command _EditarCommand { get; private set; }

        public Command EditarCommand => _EditarCommand ??= new Command(async () => await Editar(), () => TemItemSelecionado);

        public Command _AddNewCommand { get; private set; }

        public Command AddNewCommand => _AddNewCommand ??= new Command(async () => await AddNew(), () => PodeInserir);


        public void AtualizaPermissoesComandos()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _AddNewCommand?.ChangeCanExecute();
                _EditarCommand?.ChangeCanExecute();
                _VerDetalhesCommand?.ChangeCanExecute();
            });
        }

        // FIX: SfDataGrid (MAUI) usa SelectedRow em vez de SelectedItem
        public bool TemItemSelecionado => ListView != null && ListView.SelectedRow != null && ListView.SelectedIndex >= 0;

        public virtual bool PodeInserir => true;

        public virtual async Task AddNew()
        {
            HapticHelper.VibrateClick();
        }

        public virtual async Task VerDetalhes()
        {
        }

        public virtual async Task Editar()
        {
        }

        public virtual async Task ShowBusyIndicator()
        {
            busyindicator.IsVisible = true;
            busyindicator.Opacity = 1;
            busyindicator.IsRunning = true;
            dados.IsVisible = false;
        }

        public virtual async Task HideBusyIndicator()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                dados.IsVisible = true;

                await busyindicator.FadeToAsync(0)
                    .ConfigureAwait(false);
                busyindicator.IsVisible = false;
                busyindicator.IsRunning = false;
            }).ConfigureAwait(true);
        }

        public async Task OnAppearingBase()
        {
            GetViewDefaultComponentsOnAppearing();

            if (CanReloadData) _ = ShowBusyIndicator();

            await LoadData();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = OnAppearingInternalAsync();
        }

        private async Task OnAppearingInternalAsync()
        {
            try
            {
                await OnAppearingBase();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContentPageView] Erro em OnAppearing: {ex.Message}");
            }
        }

        public async Task LoadData()
        {
            if (CanReloadData)
            {
                await ShowBusyIndicator();

                await LoadPageDataAsync()
                    .ConfigureAwait(true);


                MainThread.BeginInvokeOnMainThread(() => SearchBar.Text = "");

                await HideBusyIndicator()
                    .ConfigureAwait(true);


                UpdateButtonsCanExecute();
            }

            CanReloadData = true;
        }

        public virtual void GetViewDefaultComponentsOnAppearing()
        {
            throw new Exception(Traducao.OverrideGetViewDefaultComponents);
        }

        public virtual void UpdateButtonsCanExecute()
        {
        }

        public virtual async Task LoadPageDataAsync()
        {
        }

        public virtual void RefreshData()
        {
        }

        public virtual void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshData();
        }
    }
}
