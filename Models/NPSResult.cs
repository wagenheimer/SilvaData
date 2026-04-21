namespace SilvaData.Models
{
    /// <summary>
    /// Resultado da avaliação NPS.
    /// </summary>
    public class NPSResult
    {
        /// <summary>
        /// Nota dada pelo usuário (0-10).
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        /// Comentários adicionais fornecidos pelo usuário.
        /// </summary>
        public string Comments { get; set; } = string.Empty;

        /// <summary>
        /// Cria um novo resultado NPS com valores padrão.
        /// </summary>
        public static NPSResult Default() => new()
        {
            Rating = 0,
            Comments = string.Empty
        };
    }

}
