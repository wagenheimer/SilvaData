using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

namespace SilvaData.Utils
{
    /// <summary>
    /// Service for picking photos from gallery using Android Photo Picker.
    /// No READ_MEDIA_IMAGES/READ_MEDIA_VIDEO permissions required.
    /// </summary>
    public static class PhotoPickerService
    {
        /// <summary>
        /// Picks a photo from gallery using Android Photo Picker.
        /// </summary>
        /// <returns>FileResult of selected photo or null if cancelled.</returns>
        public static async Task<FileResult?> PickPhotoAsync()
        {
            try
            {
                var options = new Microsoft.Maui.Media.MediaPickerOptions
                {
                    Title = Traducao.SelecioneUmaFoto
                };

                // On Android and iOS, MediaPicker opens the native gallery.
                // On Android 13+, it uses the new Photo Picker which doesn't require media permissions.
                var result = await Microsoft.Maui.Media.MediaPicker.Default.PickPhotoAsync(options);
                
                if (result != null)
                {
                    Debug.WriteLine($"[PhotoPickerService] ✅ Foto selecionada: {result.FileName}");
                    return result;
                }
                
                Debug.WriteLine("[PhotoPickerService] ⚠️ Nenhuma foto selecionada");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhotoPickerService] ❌ Erro ao selecionar foto: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Shows a dialog to choose between Camera and Gallery.
        /// </summary>
        /// <returns>true for camera, false for gallery, null if cancelled.</returns>
        public static async Task<bool?> ShowPhotoSourceDialogAsync()
        {
            try
            {
                var page = Application.Current!.Windows[0].Page!;
                
                // Using localized strings with emojis
                string action = await page.DisplayActionSheetAsync(
                    Traducao.AdicionarFoto,
                    Traducao.Cancelar,
                    null,
                    $"📷 {Traducao.Camera}",
                    $"🖼️ {Traducao.Galeria}"
                );

                if (action == $"📷 {Traducao.Camera}") return true;
                if (action == $"🖼️ {Traducao.Galeria}") return false;
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhotoPickerService] ❌ Erro ao mostrar diálogo: {ex.Message}");
                return null;
            }
        }
    }
}
