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
        private bool _isClosing;
        public List<ModeloIsiMacroComParametros> Modelos { get; private set; }

        // Construtor sem argumentos: permite registro como Singleton DI e prÃ©-aquecimento no startup.
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
        /// Atualiza a lista de modelos em uma instÃ¢ncia jÃ¡ construÃ­da (reuso do Singleton).
        /// Deve ser chamado antes de ShowPopupAsync quando a instÃ¢ncia Ã© reutilizada.
        /// </summary>
        public void UpdateModelos(List<ModeloIsiMacroComParametros> modelos)
        {
            Modelos = modelos ?? new();
            ModelosCollectionView.ItemsSource = Modelos;
            _selectedModelo = Modelos.Count > 0 ? Modelos[0] : null;
            ModelosCollectionView.SelectedItem = _selectedModelo;
        }

        /// <summary>
        /// âœ… Manipula a mudanÃ§a de seleÃ§Ã£o na CollectionView
        /// </summary>
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedModelo = e.CurrentSelection.FirstOrDefault() as ModeloIsiMacroComParametros;
        }

        /// <summary>
        /// âœ… Confirma seleÃ§Ã£o
        /// </summary>
        private void OnConfirmClicked(object sender, EventArgs e)
        {
            _ = OnConfirmClickedInternalAsync();
        }

        private async Task OnConfirmClickedInternalAsync() { if (_isClosing) return;
            if (_selectedModelo == null)
            {
                await PopUpOK.ShowAsync(Traducao.AtenÃ§Ã£o, Traducao.SelecioneUmModeloISIMacro);
                return;
            }

            _isClosing = true; try { await CloseAsync(_selectedModelo); } catch { _isClosing = false; }
        }

        /// <summary>
        /// âœ… Cancela popup
        /// </summary>
        private void OnCancelClicked(object sender, EventArgs e)
        {
            _ = OnCancelClickedInternalAsync();
        }

        private async Task OnCancelClickedInternalAsync() { if (_isClosing) return; _isClosing = true;
            try { await CloseAsync(null!); } catch { }
        }
    }
}

