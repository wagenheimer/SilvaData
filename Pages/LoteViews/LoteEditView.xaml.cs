using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    /// <summary>
    /// View para editar ou criar um Lote.
    /// Herda de <see cref="ContentPageEdit"/> para compatibilidade de navegação
    /// e delega toda a lógica para <see cref="LoteEditViewModel"/>.
    /// </summary>
    public partial class LoteEditView : ContentPageEdit
    {
        private new readonly LoteEditViewModel ViewModel;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="LoteEditView"/>.
        /// </summary>
        /// <param name="loteAtual">O lote a ser editado, ou null para criar um novo.</param>
        /// <param name="retomarEmAndamento">Se true, carrega dados do 'FormularioEmAndamento' (do LoteViewModel).</param>
        public LoteEditView(Lote? loteAtual = null, bool retomarEmAndamento = false)
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<LoteEditViewModel>();

            // MELHORIA: Passa apenas os dados (Lote) e o estado (retomar), não a View (this).
            ViewModel.SetInitialState(loteAtual, retomarEmAndamento);

            BindingContext = ViewModel;

            // Popula a lista de campos obrigatórios (IValidatablePage)
            // (Esta ainda é a forma como o BaseEditViewModel funciona)
            var campoNome = this.FindByName<ISITextField>("campoNomeLote");
            if (campoNome != null) RequiredInputFields.Add(campoNome);

            var comboUE = this.FindByName<UnidadeEpidemiologicaComboBox>("unidadeComboBox");
            if (comboUE != null) RequiredInputFields.Add(comboUE);
        }

        /// <summary>
        /// Garante que o ViewModel carregue os dados quando a página aparecer.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
    }
}
