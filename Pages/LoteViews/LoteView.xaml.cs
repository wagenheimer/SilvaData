using ISIInstitute.Views.LoteViews;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.ViewModels;

using System.Diagnostics;

namespace SilvaData.Controls
{
    public partial class LoteView : ContentView
    {
        private readonly LoteViewModel ViewModel;

        // Flag para garantir que os lotes sejam carregados apenas uma vez
        private bool _hasLoadedData = false;

        public LoteView()
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<LoteViewModel>();
            ViewModel.SfListView = sfListView; // Passa a referência da ListView
            BindingContext = ViewModel;

            // MELHORIA: Adiciona o manipulador de evento 'Loaded'
            this.Loaded += LoteView_Loaded;
        }

        /// <summary>
        /// Chamado quando a View é carregada pela primeira vez (ex: ao trocar de aba).
        /// </summary>
        private void LoteView_Loaded(object? sender, EventArgs e)
        {
            // Se os dados ainda não foram carregados, chama o comando.
            if (!_hasLoadedData && ViewModel != null)
            {
                _hasLoadedData = true;

                // Inicia o carregamento dos lotes em segundo plano
                // para não bloquear a UI
                Task.Run(async () =>
                {
                    if (ViewModel.CarregaLotesCommand.CanExecute(null))
                    {
                        await ViewModel.CarregaLotesCommand.ExecuteAsync(null);
                    }
                });
            }
        }

        private void OnLoteItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
        {
            if (e.DataItem is not Lote lote) return;
            Debug.WriteLine($"[LoteView] OnLoteItemTapped — lote={lote.numero}, IsBusy={ViewModel.IsBusy}");
            ViewModel.VaiParaLoteCommand.Execute(lote);
            // Limpa seleção após breve highlight (feedback visual sem seleção permanente)
            _ = Task.Delay(300).ContinueWith(_ =>
                MainThread.BeginInvokeOnMainThread(() => sfListView.SelectedItem = null));
        }
    }
}
