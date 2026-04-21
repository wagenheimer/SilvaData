using Microsoft.Maui.Graphics;

namespace SilvaData.Controls
{
    public static class ValidationVisualHelper
    {
        public static Color GetPrimaryColor()
        {
            if (Application.Current?.Resources != null &&
                Application.Current.Resources.TryGetValue("PrimaryColor", out var color) &&
                color is Color primaryColor)
            {
                return primaryColor;
            }

            return Colors.Blue;
        }

        public static void ApplyTitleColor(Label? label, bool hasError)
        {
            if (label == null)
            {
                return;
            }

            label.TextColor = hasError ? Colors.Red : GetPrimaryColor();
        }
    }
}
