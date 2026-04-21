using DateChangedEventArgs = Microsoft.Maui.Controls.DateChangedEventArgs;

using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    public partial class LoteFormHeader : ContentView
    {
        // Construtor sem parâmetros necessário para instanciação via XAML
        public LoteFormHeader()
        {
            InitializeComponent();
        }

        // Construtor opcional com ViewModel para cenários de DI
        public LoteFormHeader(LoteFormularioViewModel loteFormViewModel)
        {
            InitializeComponent();
            BindingContext = loteFormViewModel;
        }

        private void Date_Picker_OnDateChanged(object sender, DateChangedEventArgs e)
        {
            if (BindingContext is LoteFormularioViewModel vm)
            {
                vm.UpdateIdadeLote();
            }
        }
    }
}
