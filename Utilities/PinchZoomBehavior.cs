using System;
using Microsoft.Maui.Controls;

namespace SilvaData.Utilities
{
    /// <summary>
    /// Fornece funcionalidade de Pinch-to-Zoom e Pan para controles que suportam GestureRecognizers.
    /// Ideal para visualização de fotos no iOS onde componentes de terceiros podem ter falhas de renderização.
    /// </summary>
    public class PinchZoomBehavior : Behavior<View>
    {
        private double _currentScale = 1;
        private double _startScale = 1;
        private double _xOffset = 0;
        private double _yOffset = 0;

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);

            var pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;
            bindable.GestureRecognizers.Add(pinchGesture);

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            bindable.GestureRecognizers.Add(panGesture);

            bindable.PropertyChanged += OnPropertyChanged;
        }

        protected override void OnDetachingFrom(View bindable)
        {
            bindable.PropertyChanged -= OnPropertyChanged;
            base.OnDetachingFrom(bindable);
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Image.SourceProperty.PropertyName)
            {
                var view = (View)sender;
                view.Scale = 1;
                view.TranslationX = 0;
                view.TranslationY = 0;
                _currentScale = 1;
                _xOffset = 0;
                _yOffset = 0;
            }
        }


        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            var view = (View)sender;
            switch (e.Status)
            {
                case GestureStatus.Started:
                    _startScale = view.Scale;
                    // Define a âncora no centro do gesto de pinça
                    view.AnchorX = e.ScaleOrigin.X;
                    view.AnchorY = e.ScaleOrigin.Y;
                    break;
                case GestureStatus.Running:
                    // Calcula a nova escala com base no delta
                    double delta = e.Scale;
                    _currentScale += (delta - 1) * _startScale;
                    _currentScale = Math.Clamp(_currentScale, 1, 10);

                    view.Scale = _currentScale;
                    break;
                case GestureStatus.Completed:
                    if (view.Scale < 1.05)
                    {
                        Reset(view);
                    }
                    else
                    {
                        // Normaliza âncora de volta para o centro (0.5, 0.5) e compensa
                        // com TranslationX/Y para manter a posição visual idêntica.
                        // Sem isso, _xOffset=0 mas a imagem está visualmente deslocada
                        // pela âncora não-central, quebrando o pan subsequente.
                        double w = view.Width;
                        double h = view.Height;
                        double s = view.Scale;

                        double dx = (0.5 - view.AnchorX) * w * (s - 1);
                        double dy = (0.5 - view.AnchorY) * h * (s - 1);

                        view.TranslationX += dx;
                        view.TranslationY += dy;
                        view.AnchorX = 0.5;
                        view.AnchorY = 0.5;

                        double maxX = w * (s - 1) / 2;
                        double maxY = h * (s - 1) / 2;
                        view.TranslationX = Math.Clamp(view.TranslationX, -maxX, maxX);
                        view.TranslationY = Math.Clamp(view.TranslationY, -maxY, maxY);

                        _xOffset = view.TranslationX;
                        _yOffset = view.TranslationY;
                    }
                    break;
            }
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            var view = (View)sender;
            if (view.Scale <= 1.01) return;

            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    // Limita a tradução para que a foto não suma da tela
                    double maxTranslationX = view.Width * (view.Scale - 1) / 2;
                    double maxTranslationY = view.Height * (view.Scale - 1) / 2;

                    double newX = _xOffset + e.TotalX;
                    double newY = _yOffset + e.TotalY;

                    view.TranslationX = Math.Clamp(newX, -maxTranslationX, maxTranslationX);
                    view.TranslationY = Math.Clamp(newY, -maxTranslationY, maxTranslationY);
                    break;
                case GestureStatus.Completed:
                    _xOffset = view.TranslationX;
                    _yOffset = view.TranslationY;
                    break;
            }
        }


        private void Reset(View view)
        {
            view.Scale = 1;
            view.TranslationX = 0;
            view.TranslationY = 0;
            view.AnchorX = 0.5;
            view.AnchorY = 0.5;
            _currentScale = 1;
            _xOffset = 0;
            _yOffset = 0;
        }

    }
}
