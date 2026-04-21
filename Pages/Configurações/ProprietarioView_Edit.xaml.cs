using CommunityToolkit.Mvvm.Messaging;

using SilvaData_MAUI.Infrastructure;
using SilvaData_MAUI.Models;
using SilvaData_MAUI.Utilities;
using SilvaData_MAUI.ViewModels;

namespace SilvaData_MAUI.Controls
{
    /// <summary>
    /// View para editar ou criar um Proprietário.
    /// MIGRADO: Usa Messaging para comunicação com o ViewModel (sem acoplamento direto).
    /// </summary>
    public partial class ProprietarioView_Edit : ContentPageEdit
    {
        private new readonly ProprietarioEditViewModel ViewModel;

        /// <summary>
        /// MIGRADO: Construtor não passa mais IValidatablePage para o ViewModel
        /// </summary>
        public ProprietarioView_Edit(Proprietario? proprietario = null)
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<ProprietarioEditViewModel>();

            // MUDANÇA: Não passa mais 'this' (IValidatablePage) para o SetInitialState
            ViewModel.SetInitialState(proprietario);

            BindingContext = ViewModel;

            RequiredInputFields.Add(this.FindByName<ISITextField>("nome"));
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}
