using SilvaData.Models;

namespace SilvaData.Utilities
{
    public static class LoteExtensions
    {
        public static void EnsureNames(this Lote lote, CacheService cache)
        {
            if (lote == null || cache == null) return;

            // Lote -> UE -> Propriedade -> Regional
            if (lote.unidadeEpidemiologicaId.HasValue)
            {
                var ue = cache.GetUE(lote.unidadeEpidemiologicaId.Value);
                if (ue != null)
                {
                    if (string.IsNullOrWhiteSpace(lote.UnidadeEpidemiologicaNome))
                        lote.UnidadeEpidemiologicaNome = ue.nome;

                    if (string.IsNullOrWhiteSpace(lote.PropriedadeNome))
                        lote.PropriedadeNome = ue.PropriedadeNome;

                    if (string.IsNullOrWhiteSpace(lote.RegionalNome))
                        lote.RegionalNome = ue.RegionalNome;
                }
            }
        }

        public static void EnsureNames(this IEnumerable<Lote> lotes, CacheService cache)
        {
            if (lotes == null) return;
            foreach (var lote in lotes)
                EnsureNames(lote, cache);
        }

        public static void EnsureNames(this Lote lote)
        {
            var cache = ServiceHelper.GetRequiredService<CacheService>();
            EnsureNames(lote, cache);
        }

        public static void EnsureNames(this IEnumerable<Lote> lotes)
        {
            var cache = ServiceHelper.GetRequiredService<CacheService>();
            EnsureNames(lotes, cache);
        }

        public static string GetUEName(this Lote lote, CacheService cache)
        {
            EnsureNames(lote, cache);
            return lote.UnidadeEpidemiologicaNome;
        }

        public static string GetPropriedadeName(this Lote lote, CacheService cache)
        {
            EnsureNames(lote, cache);
            return lote.PropriedadeNome;
        }

        public static string GetRegionalName(this Lote lote, CacheService cache)
        {
            EnsureNames(lote, cache);
            return lote.RegionalNome;
        }
    }
}
