using Microsoft.Maui.Controls.Xaml;
using LocalizationResourceManager.Maui;

namespace SilvaData.Extensions
{
    // Wrapper do TranslateExtension da lib LocalizationResourceManager.Maui.
    // Mantém o namespace safe: nos XAMLs. A validação de chaves faltantes
    // é feita em compile-time pelo script Build/ValidateTranslationKeys.ps1.
    [ContentProperty(nameof(Key))]
    public class SafeTranslateExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
            => new TranslateExtension { Text = Key }.ProvideValue(serviceProvider);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
            => ProvideValue(serviceProvider);
    }
}
