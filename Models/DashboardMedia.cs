using CommunityToolkit.Mvvm.ComponentModel; // Necessário para ObservableObject

namespace SilvaData_MAUI.Models
{
    /// <summary>
    /// Modelo de dados (DTO) que armazena as médias
    /// exibidas nos cartões do Dashboard (Home).
    /// </summary>
    public partial class DashboardMedia : ObservableObject
    {
        // Propriedades permanecem, mas podem ser ObservableProperties
        // se você precisar de binding granular.
        // Se você sempre substitui o objeto inteiro, { get; set; } é suficiente.
        public double mediaIsiMacroClienteUsuario { get; set; }
        public double mediaIsiMacroCliente { get; set; }
        public double mediaIsiMacroGlobal { get; set; }
        public double mediaScoreManejo { get; set; }
        public double mediaIEP { get; set; }
    }
}