using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    public partial class SincronizacaoPendentesView : ContentView
    {
        public SincronizacaoPendentesView()
        {
            InitializeComponent();

            // ??? Usa ServiceHelper para obter ViewModel do DI ???
            BindingContext = ServiceHelper.GetRequiredService<SincronizacaoPendentesViewModel>();
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();
            // Auto-atualiza sempre que a view entra na árvore visual
            TryRefresh();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            // Garantia extra para cenários de recriação do handler
            TryRefresh();
        }

        private void TryRefresh()
        {
            if (BindingContext is SincronizacaoPendentesViewModel vm)
            {
                var cmd = vm.AtualizaListaAlteracoesCommand;
                if (cmd?.CanExecute(null) == true)
                {
                    // dispara assíncrono sem bloquear a UI
                    _ = vm.AtualizaListaAlteracoesCommand.ExecuteAsync(null);
                }
            }
        }
    }
}
