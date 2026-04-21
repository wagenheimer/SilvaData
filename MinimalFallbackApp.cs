using Microsoft.Maui.Controls;
using System;

namespace SilvaData
{
    // Aplicação mínima usada como fallback quando a inicialização falha.
    public class MinimalFallbackApp : Application
    {
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var message = new Label
            {
                Text = "O aplicativo encontrou um erro ao iniciar. Verifique os logs para mais detalhes.",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(20)
            };

            return new Window(new ContentPage
            {
                Title = "Erro",
                Content = new ScrollView
                {
                    Content = new StackLayout
                    {
                        Children = { message }
                    }
                }
            });
        }
    }
}
