using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    /// <summary>
    /// View para editar ou criar uma Regional.
    /// Herda de <see cref="ContentPageEdit"/> para compatibilidade de navegação
    /// e delega toda a lógica para <see cref="RegionalEditViewModel"/>.
    /// </summary>
    public partial class RegionalView_Edit : ContentPageEdit
    {
        private new readonly RegionalEditViewModel ViewModel;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="RegionalView_Edit"/>.
        /// </summary>
        /// <param name="regional">A regional a ser editada, ou null para criar uma nova.</param>
        public RegionalView_Edit(Regional? regional = null)
        {
            InitializeComponent();

            // Obtém o ViewModel (Regra 2)
            ViewModel = ServiceHelper.GetRequiredService<RegionalEditViewModel>();

            // Passa o parâmetro para o ViewModel
            ViewModel.SetInitialState(regional);

            // Define o BindingContext da página
            BindingContext = ViewModel;

            var campoNome = this.FindByName<ISITextField>("nome");
            if (campoNome != null) RequiredInputFields.Add(campoNome);
        }
    }
}