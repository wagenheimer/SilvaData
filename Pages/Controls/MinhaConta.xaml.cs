using SilvaData_MAUI.Infrastructure;
using SilvaData_MAUI.Models;
using SilvaData_MAUI.ViewModels;
using SilvaData_MAUI.Utils; // Adicionado

namespace ISIInstitute.Views // (Mantendo seu namespace original)
{
    /// <summary>
    /// View (Página) para a tela "Minha Conta".
    /// </summary>
    public partial class MinhaConta : ContentPageWithLocalization
    {
        private readonly MinhaContaViewModel ViewModel;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="MinhaConta"/>.
        /// </summary>
        public MinhaConta()
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<MinhaContaViewModel>();
            BindingContext = ViewModel;
        }
    }
}