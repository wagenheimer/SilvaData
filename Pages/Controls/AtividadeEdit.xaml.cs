using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

using System.Diagnostics;

namespace SilvaData.Controls
{
    /// <summary>
    /// View para editar ou criar uma Atividade.
    /// MIGRADO: Usa Messaging para comunicação com o ViewModel (sem acoplamento direto).
    /// </summary>
    public partial class AtividadeEdit : ContentPageEdit
    {
        private new readonly AtividadeEditViewModel ViewModel;

        /// <summary>
        /// MIGRADO: Construtor não passa mais IValidatablePage para o ViewModel
        /// </summary>
        public AtividadeEdit(int id = -1)
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<AtividadeEditViewModel>();

            // MUDANÇA: Não passa mais 'this' para o SetInitialState
            ViewModel.SetInitialState(id);

            BindingContext = ViewModel;

            // Popula a lista de campos obrigatórios para a validação da UI
            RequiredInputFields.Add(this.FindByName<ISITextField>("titulo"));
            RequiredInputFields.Add(this.FindByName<UnidadeEpidemiologicaComboBox>("unidadeComboBox"));
            RequiredInputFields.Add(this.FindByName<ISIDatePicker>("dataInicio"));
            RequiredInputFields.Add(this.FindByName<ISIDatePicker>("dataPrazo"));

            // Registra os handlers para o ciclo de vida da View
            this.Loaded += OnPageLoaded;
            this.Unloaded += OnPageUnloaded;
        }

        #region Ciclo de Vida e Handlers de Mensagem

        /// <summary>
        /// Chamado quando a página é carregada.
        /// </summary>
        private void OnPageLoaded(object? sender, EventArgs e)
        {
            // Mensagens específicas desta View podem ser registradas aqui
        }

        /// <summary>
        /// Chamado quando a página é descarregada (destruída).
        /// </summary>
        private void OnPageUnloaded(object? sender, EventArgs e)
        {
            // Mensagens registradas em OnPageLoaded devem ser desregistradas aqui
        }

        #endregion

    }
}
