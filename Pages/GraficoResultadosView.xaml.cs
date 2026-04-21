using SilvaData.ViewModels;

using System;

namespace SilvaData.Controls
{
    /// <summary>
    /// Code-behind for the ContentView that hosts the charts in the "Meus Resultados" tab.
    /// </summary>
    public partial class GraficoResultadosView : ContentView
    {
        // Safe accessor to the ViewModel
        private GraficoResultadosViewModel? ViewModel => BindingContext as GraficoResultadosViewModel;

        public GraficoResultadosView()
        {
            InitializeComponent();
            // The BindingContext is set by the parent (DashboardView)
        }

        /// <summary>
        /// Update the ViewModel with current device orientation.
        /// </summary>
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            ViewModel?.AtualizarOrientacao(width, height);
        }
    }
}
