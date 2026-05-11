using SilvaData.Infrastructure;
using SilvaData.ViewModels;
using System.ComponentModel;
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
            
            var viewModel = ServiceHelper.GetRequiredService<SincronizacaoViewModel>();
            BindingContext = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SincronizacaoViewModel.SincronizacaoComSucesso))
            {
                if (BindingContext is SincronizacaoViewModel vm && vm.SincronizacaoComSucesso)
                {
                    await AnimateSuccessView();
                }
            }
        }

        private async Task AnimateSuccessView()
        {
            if (SuccessView == null) return;

            // Prepara o estado inicial
            SuccessView.Opacity = 0;
            SuccessView.Scale = 0.8;
            SuccessView.TranslationY = 50;

            // Animação de entrada
            await Task.WhenAll(
                SuccessView.FadeTo(1, 600, Easing.CubicOut),
                SuccessView.ScaleTo(1, 600, Easing.CubicOut),
                SuccessView.TranslateTo(0, 0, 600, Easing.CubicOut)
            );
        }
    }
}
