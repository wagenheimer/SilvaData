namespace SilvaData.Resources
{
    /// <summary>
    /// Helper para acesso simplificado a textos localizados
    /// </summary>
    public static class LocalizationHelper
    {
        /// <summary>
        /// Obtém um texto localizado formatado com os argumentos fornecidos
        /// </summary>
        /// <param name="resourceKey">Chave do recurso de localização</param>
        /// <param name="args">Argumentos para formatação</param>
        /// <returns>Texto localizado e formatado</returns>
        public static string Format(string resourceKey, params object[] args)
        {
            // Obtém a propriedade do recurso usando reflexão
            var property = typeof(Localization.Localization).GetProperty(resourceKey);
            if (property == null)
                return $"[{resourceKey}]";
                
            var value = property.GetValue(null) as string;
            if (string.IsNullOrEmpty(value))
                return $"[{resourceKey}]";
                
            // Formata o texto com os argumentos fornecidos
            if (args != null && args.Length > 0)
                return string.Format(value, args);
                
            return value;
        }
    }
}
