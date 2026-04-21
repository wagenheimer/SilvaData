using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;

using System.Diagnostics;

namespace SilvaData.Utilities
{
    /// <summary>
    /// Helper otimizado para centralizar as chamadas de vibração/haptic feedback.
    /// </summary>
    public static class HapticHelper
    {
        /// <summary>
        /// Executa um feedback tátil rápido de "clique" ou erro de validação.
        /// </summary>
        public static void VibrateClick()
        {
            ExecuteHaptic(HapticFeedbackType.Click, TimeSpan.FromMilliseconds(40));
        }

        /// <summary>
        /// Executa um feedback tátil mais longo para sucesso de gravação.
        /// </summary>
        public static void VibrateSuccess()
        {
            // Usando LongPress no iOS para diferenciar do clique normal
            ExecuteHaptic(HapticFeedbackType.LongPress, TimeSpan.FromMilliseconds(80));
        }

        /// <summary>
        /// Método central para lidar com a plataforma, suporte de hardware e exceções.
        /// </summary>
        private static void ExecuteHaptic(HapticFeedbackType iosType, TimeSpan androidDuration)
        {
            try
            {
                if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
                {
                    if (HapticFeedback.Default.IsSupported)
                    {
                        HapticFeedback.Default.Perform(iosType);
                    }
                    else
                    {
                        Debug.WriteLine("[HapticHelper] HapticFeedback não suportado neste dispositivo iOS.");
                    }
                }
                else // Assume Android ou outras plataformas onde Vibration foi solicitado
                {
                    if (Vibration.Default.IsSupported)
                    {
                        Vibration.Default.Vibrate(androidDuration);
                    }
                    else
                    {
                        Debug.WriteLine("[HapticHelper] Vibração não suportada neste dispositivo Android.");
                    }
                }
            }
            catch (FeatureNotSupportedException ex)
            {
                Debug.WriteLine($"[HapticHelper] Recurso não suportado no hardware: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HapticHelper] Erro ao vibrar: {ex.Message}");
            }
        }
    }
}