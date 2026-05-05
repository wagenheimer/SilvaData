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
            ViewModel.SfListView = sfListView;
            ViewModel.NovoLoteAdicionado += OnNovoLoteAdicionado;
            BindingContext = ViewModel;

            this.Loaded += LoteView_Loaded;
        }

        private void OnNovoLoteAdicionado(Lote lote)
        {
            _ = ScrollAndHighlightAsync(lote);
        }

        private async Task ScrollAndHighlightAsync(Lote lote)
        {
            try
            {
                await Task.Delay(150);
                sfListView.ScrollTo(lote, ScrollToPosition.Start, true);
                await Task.Delay(350);
                sfListView.SelectedItem = lote;
                await Task.Delay(800);
                sfListView.SelectedItem = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteView] ScrollAndHighlight erro: {ex.Message}");
            }
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
