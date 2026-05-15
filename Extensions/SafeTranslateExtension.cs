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
                Source = SafeLocalizationSource.Instance,
                Path = nameof(SafeLocalizationSource.Dummy),
                Converter = new SafeKeyConverter(Key),
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

        // Propriedade dummy para o binding ter um path válido
        public int Dummy { get; private set; }

        public void NotifyAllChanged()
        {
            Dummy++;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dummy)));
        }

        public static string GetString(string key)
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

    public class SafeKeyConverter : IValueConverter
    {
        private readonly string _key;
        public SafeKeyConverter(string key) => _key = key;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => SafeLocalizationSource.GetString(_key);

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
