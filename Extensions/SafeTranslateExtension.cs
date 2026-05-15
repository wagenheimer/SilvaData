using Microsoft.Maui.Controls.Xaml;
using Sentry;

namespace SilvaData.Extensions
{
    [ContentProperty(nameof(Key))]
    public class SafeTranslateExtension : IMarkupExtension<string>
    {
        public string Key { get; set; }

        public string ProvideValue(IServiceProvider serviceProvider)
        {
            try
            {
                var value = Traducao.ResourceManager.GetString(Key, Traducao.Culture);
                if (value is null)
                {
                    ReportMissingKey(Key);
                    return $"[MISSING:{Key}]";
                }
                return value;
            }
            catch (Exception ex)
            {
                ReportTranslationError(Key, ex);
                return $"[ERR:{Key}]";
            }
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
            => ProvideValue(serviceProvider);

        private static void ReportMissingKey(string key)
        {
            System.Diagnostics.Debug.WriteLine($"[SafeTranslate] Chave ausente: {key}");
            SentrySdk.CaptureMessage(
                $"Tradução ausente: '{key}'",
                scope =>
                {
                    scope.SetTag("localization.key", key);
                    scope.SetTag("localization.culture", Traducao.Culture?.Name ?? "default");
                    scope.Level = SentryLevel.Warning;
                });
        }

        private static void ReportTranslationError(string key, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SafeTranslate] Erro ao traduzir '{key}': {ex}");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("localization.key", key);
                scope.SetTag("localization.culture", Traducao.Culture?.Name ?? "default");
            });
        }
    }
}
