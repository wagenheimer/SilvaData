namespace SilvaData.Controls
{
    /// <summary>
    /// Define uma interface para uma View que pode validar a si mesma.
    /// </summary>
    public interface IValidatablePage
    {
        /// <summary>
        /// Executa a validação da UI e retorna true se for válida.
        /// </summary>
        Task<bool> ValidateFormAsync();
    }
}
