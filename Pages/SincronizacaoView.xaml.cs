using SilvaData.Infrastructure;
using SilvaData.ViewModels;

using Microsoft.Maui.Controls;

namespace SilvaData.Controls
{
    /// <summary>
    /// View (ContentView) para exibir o progresso do Download.
    /// </summary>
    public partial class SincronizacaoView : ContentView
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="SincronizacaoView"/>.
        /// </summary>
        public SincronizacaoView()
        {
            InitializeComponent();
            // O BindingContext é definido aqui, como no seu código original
            BindingContext = ServiceHelper.GetRequiredService<SincronizacaoViewModel>();
        }
    }
}
