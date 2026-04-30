using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Utils;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.Pages.PopUps
{
    public partial class VerRegistrosPopup : Popup<LoteFormAvaliacaoGalpao>
    {
        private readonly VerRegistrosPopupViewModel _viewModel;

        public VerRegistrosPopup(ObservableCollection<LoteFormAvaliacaoGalpao> registros, bool isQualitativo)
        {
            InitializeComponent();
            
            // Cria e configura o ViewModel
            _viewModel = new VerRegistrosPopupViewModel(registros, isQualitativo);
            BindingContext = _viewModel;

            Debug.WriteLine($"[VerRegistrosPopup] Popup inicializado - Qualitativo: {isQualitativo}");
        }

        private async void OnCloseButtonClicked(object sender, EventArgs e)
        {
            await CloseAsync(null!);
        }

        /// <summary>
        /// Manipula o toque em um item da lista (qualitativo ou quantitativo)
        /// </summary>
        private async void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
        {
            try
            {
                // ValidaÃ§Ãµes defensivas
                if (e?.DataItem == null)
                {
                    Debug.WriteLine("[VerRegistrosPopup] âš ï¸ ItemTappedEventArgs ou DataItem Ã© nulo");
                    return;
                }

                if (e.DataItem is not LoteFormAvaliacaoGalpao registro)
                {
                    Debug.WriteLine($"[VerRegistrosPopup] âš ï¸ DataItem nÃ£o Ã© LoteFormAvaliacaoGalpao: {e.DataItem?.GetType().Name}");
                    return;
                }

                Debug.WriteLine($"[VerRegistrosPopup] ðŸ“Š Registro selecionado: {registro.NumeroResposta}");

                // Feedback tÃ¡til imediato
                HapticHelper.VibrateClick();

                // Fecha o popup com o registro selecionado
                await CloseAsync(registro);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerRegistrosPopup] âŒ Erro ao processar item selecionado: {ex.Message}");
                
                // Recovery: tenta fechar o popup mesmo com erro
                try
                {
                    await CloseAsync(null!);
                }
                catch (Exception recoveryEx)
                {
                    Debug.WriteLine($"[VerRegistrosPopup] ðŸš¨ Recovery falhou: {recoveryEx.Message}");
                }
            }
        }

        /// <summary>
        /// Override do mÃ©todo CloseAsync para incluir logging
        /// </summary>
        public new async Task CloseAsync(LoteFormAvaliacaoGalpao? result) { if (_isClosing) return; _isClosing = true;
            try
            {
                Debug.WriteLine($"[VerRegistrosPopup] Fechando popup - Resultado: {(result?.NumeroResposta.ToString() ?? "NULL")}");
                
                // Chama o mÃ©todo base da classe Popup
                await base.CloseAsync(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerRegistrosPopup] âŒ Erro ao fechar popup: {ex.Message}");
            }
        }

        /// <summary>
        /// MÃ©todo estÃ¡tico para facilitar o uso do popup
        /// </summary>
        /// <param name="registros">Lista de registros da avaliaÃ§Ã£o</param>
        /// <param name="isQualitativo">Indica se Ã© avaliaÃ§Ã£o qualitativa</param>
        /// <returns>Registro selecionado ou null se cancelado</returns>
        public static async Task<LoteFormAvaliacaoGalpao?> ShowAsync(
            ObservableCollection<LoteFormAvaliacaoGalpao> registros, 
            bool isQualitativo)
        {
            try
            {
                Debug.WriteLine($"[VerRegistrosPopup] Abrindo popup - Registros: {registros?.Count ?? 0}, Qualitativo: {isQualitativo}");

                var popup = new VerRegistrosPopup(registros, isQualitativo);
                var result = await NavigationUtils.ShowPopupAsync<LoteFormAvaliacaoGalpao>(popup);
                
                Debug.WriteLine($"[VerRegistrosPopup] Popup fechado - Resultado: {result?.NumeroResposta.ToString() ?? "NULL"}");
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerRegistrosPopup] âŒ Erro ao mostrar popup: {ex.Message}");
                return null;
            }
        }
    }
}

