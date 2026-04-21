namespace SilvaData.Controls
{
    /// <summary>
    /// Essa INTERFACE serve para permitir o controle de campos obrigatórios no Form
    /// </summary>
    public interface ICampoObrigatorio
    {
        /// <summary>
        /// Função Obrigatória para Verificar se o Campo Está Preenchido Corratamente
        /// Nesta função é necessário setar o hasError do sfInputField
        /// </summary>
        bool PreenchidoCorretamente();
    }
}
