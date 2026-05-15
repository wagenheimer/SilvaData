using System.ComponentModel;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Xaml;
using LocalizationResourceManager.Maui;
using Sentry;

namespace SilvaData.Extensions
{
    // Replica o comportamento da lib LocalizationResourceManager.Maui mas com try/catch
    // para não crashar quando a chave não existe (iOS Release/AOT).
    [ContentProperty(nameof(Key))]
    public class SafeTranslateExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            // Obtém o ILocalizationResourceManager do DI — mesmo source que a lib original usa
            var locManager = serviceProvider
                .GetRequiredService<IServiceProvider>()
                .GetService<ILocalizationResourceManager>()
                ?? Application.Current?.Handler?.MauiContext?.Services
                    .GetService<ILocalizationResourceManager>();

            if (locManager is null)
            {
                // Fallback: retorna string direta se DI ainda não estiver pronto
                return new Binding { Source = GetStringSafe(Key), Mode = BindingMode.OneTime };
            }

            return new Binding
            {
                Mode = BindingMode.OneWay,
                Source = locManager,
                Path = $"[{Key}]",
                Converter = new SafeTranslateConverter(Key),
                FallbackValue = $"[MISSING:{Key}]",
                TargetNullValue = $"[MISSING:{Key}]",
            };
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
            => ProvideValue(serviceProvider);

        internal static string GetStringSafe(string key)
        {
            try
            {
                var value = Traducao.ResourceManager.GetString(key, Traducao.Culture
                    ?? CultureInfo.CurrentUICulture);
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

    // Converter que intercepta o valor do ILocalizationResourceManager e adiciona proteção
    internal class SafeTranslateConverter : IValueConverter
    {
        private readonly string _key;
        public SafeTranslateConverter(string key) => _key = key;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => SafeTranslateExtension.GetStringSafe(_key);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
