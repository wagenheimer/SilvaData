using System.Linq;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

using SilvaData.Models;
using SilvaData.Pages.PopUps;

namespace SilvaData.Pages.PopUps
{
    public partial class SelectModeloPopup : Popup<ModeloIsiMacroComParametros>
    {
        private ModeloIsiMacroComParametros? _selectedModelo;
        public List<ModeloIsiMacroComParametros> Modelos { get; private set; }

        // Construtor sem argumentos: permite registro como Singleton DI e pré-aquecimento no startup.
        public SelectModeloPopup()
        {
            InitializeComponent();
            Modelos = new();
            BindingContext = this;
        }

        public SelectModeloPopup(List<ModeloIsiMacroComParametros> modelos) : this()
        {
            UpdateModelos(modelos);
        }

        /// <summary>
        /// Atualiza a lista de modelos em uma instância já construída (reuso do Singleton).
        /// Deve ser chamado antes de ShowPopupAsync quando a instância é reutilizada.
        /// </summary>
        public void UpdateModelos(List<ModeloIsiMacroComParametros> modelos)
        {
            Modelos = modelos ?? new();
            ModelosCollectionView.ItemsSource = Modelos;
            _selectedModelo = Modelos.Count > 0 ? Modelos[0] : null;
            ModelosCollectionView.SelectedItem = _selectedModelo;
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
            if (_selectedModelo == null)
            {
                await PopUpOK.ShowAsync(Traducao.Atenção, Traducao.SelecioneUmModeloISIMacro);
                return;
            }

            await CloseAsync(_selectedModelo);
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
            await CloseAsync(null!);
        }
    }
}
