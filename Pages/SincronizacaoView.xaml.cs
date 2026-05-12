using SilvaData.Infrastructure;
using SilvaData.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace SilvaData.Controls
{
    public partial class SincronizacaoView : ContentView
    {
        public SincronizacaoView()
        {
            InitializeComponent();

            var viewModel = ServiceHelper.GetRequiredService<SincronizacaoViewModel>();
            BindingContext = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SincronizacaoViewModel.SincronizacaoComSucesso))
            {
                if (BindingContext is SincronizacaoViewModel vm && vm.SincronizacaoComSucesso)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await AnimateSuccessView());
                }
            }
        }

        private async Task AnimateSuccessView()
        {
            if (SuccessView == null) return;

            // Estado inicial do card
            SuccessView.Opacity = 0;
            SuccessView.Scale = 0.9;
            SuccessView.TranslationY = 60;

            // Estado inicial do ícone
            if (SuccessIcon != null)
            {
                SuccessIcon.Scale = 0;
                SuccessIcon.Opacity = 0;
            }

            // 1. Card entra suavemente por baixo
            await Task.WhenAll(
                SuccessView.FadeToAsync(1, 380, Easing.CubicOut),
                SuccessView.ScaleToAsync(1, 380, Easing.CubicOut),
                SuccessView.TranslateToAsync(0, 0, 380, Easing.CubicOut)
            );

            // 2. Ícone aparece com bounce pop
            if (SuccessIcon != null)
            {
                await SuccessIcon.FadeToAsync(1, 80);
                await SuccessIcon.ScaleToAsync(1.35, 180, Easing.CubicOut);
                await SuccessIcon.ScaleToAsync(0.85, 110, Easing.CubicIn);
                await SuccessIcon.ScaleToAsync(1.1, 90, Easing.CubicOut);
                await SuccessIcon.ScaleToAsync(1.0, 70, Easing.CubicIn);
            }
        }
    }
}
