using System.ComponentModel;
using Microsoft.Maui.Controls.Xaml;
using Sentry;

namespace SilvaData.Extensions
{
    [ContentProperty(nameof(Key))]
    public class SafeTranslateExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding
            {
                Mode = BindingMode.OneWay,
                Path = $"[{Key}]",
                Source = SafeLocalizationSource.Instance,
                FallbackValue = $"[MISSING:{Key}]",
                TargetNullValue = $"[MISSING:{Key}]",
            };
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
            => ProvideValue(serviceProvider);
    }

    public class SafeLocalizationSource : INotifyPropertyChanged
    {
        public static readonly SafeLocalizationSource Instance = new();

        public event PropertyChangedEventHandler PropertyChanged;

        public string this[string key]
        {
            get
            {
                try
                {
                    var value = Traducao.ResourceManager.GetString(key, Traducao.Culture);
                    if (value is null)
                    {
                        ReportMissingKey(key);
                        return $"[MISSING:{key}]";
                    }
                    return value;
                }
                catch (Exception ex)
                {
                    ReportTranslationError(key, ex);
                    return $"[ERR:{key}]";
                }
            }
        }

        public void NotifyAllChanged()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));

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
