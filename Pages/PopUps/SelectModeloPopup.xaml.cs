using CommunityToolkit.Maui.Views;
using SilvaData.Models;
using System.Collections.ObjectModel;

namespace SilvaData.Pages.PopUps
{
    public partial class SelectModeloPopup : Popup<ModeloIsiMacroComParametros>
    {
        private ModeloIsiMacroComParametros? _selectedModelo;
        private bool _isClosing;

        public ObservableCollection<ModeloIsiMacroComParametros> Modelos { get; } = new();

        public SelectModeloPopup()
        {
            InitializeComponent();
            BindingContext = this;
        }

        public SelectModeloPopup(IEnumerable<ModeloIsiMacroComParametros> modelos) : this()
        {
            foreach (var m in modelos) Modelos.Add(m);
        }

        /// <summary>
        /// ✅ Atualiza a lista de modelos (chamado por LoteISIMacroViewModel)
        /// </summary>
        public void UpdateModelos(IEnumerable<ModeloIsiMacroComParametros> modelos)
        {
            Modelos.Clear();
            foreach (var m in modelos) Modelos.Add(m);
        }

        /// <summary>
        /// ✅ Manipula a mudança de seleção na CollectionView
        /// </summary>
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedModelo = e.CurrentSelection.FirstOrDefault() as ModeloIsiMacroComParametros;
        }

        /// <summary>
        /// ✅ Confirma seleção
        /// </summary>
        private void OnConfirmClicked(object sender, EventArgs e)
        {
            _ = OnConfirmClickedInternalAsync();
        }

        private async Task OnConfirmClickedInternalAsync()
        {
            if (_isClosing) return;
            if (_selectedModelo == null)
            {
                await PopUpOK.ShowAsync(Traducao.Atenção, Traducao.SelecioneUmModeloISIMacro);
                return;
            }

            _isClosing = true;
            try
            {
                await CloseAsync(_selectedModelo);
            }
            catch
            {
                _isClosing = false;
            }
        }

        /// <summary>
        /// ✅ Cancela popup
        /// </summary>
        private void OnCancelClicked(object sender, EventArgs e)
        {
            _ = OnCancelClickedInternalAsync();
        }

        private async Task OnCancelClickedInternalAsync()
        {
            if (_isClosing) return;
            _isClosing = true;
            try
            {
                await CloseAsync(null);
            }
            catch
            {
                _isClosing = false;
            }
        }
    }
}
