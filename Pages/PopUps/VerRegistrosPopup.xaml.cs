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
        private bool _isClosing;

        public VerRegistrosPopup(ObservableCollection<LoteFormAvaliacaoGalpao> registros, bool isQualitativo, int? registroAtualNumero = null)
        {
            InitializeComponent();

            _viewModel = new VerRegistrosPopupViewModel(registros, isQualitativo, registroAtualNumero);
            BindingContext = _viewModel;

            Debug.WriteLine($"[VerRegistrosPopup] Popup inicializado - Qualitativo: {isQualitativo}");
        }

        private async void OnCloseButtonClicked(object sender, EventArgs e)
        {
            await CloseWithLogAsync(null!);
        }

        /// <summary>
        /// Manipula o toque em um item da lista (qualitativo ou quantitativo)
        /// </summary>
        private async void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
        {
            try
            {
                // Validações defensivas
                if (e?.DataItem == null)
                {
                    Debug.WriteLine("[VerRegistrosPopup] ⚠️ ItemTappedEventArgs ou DataItem é nulo");
                    return;
                }

                if (e.DataItem is not LoteFormAvaliacaoGalpao registro)
                {
                    Debug.WriteLine($"[VerRegistrosPopup] ⚠️ DataItem não é LoteFormAvaliacaoGalpao: {e.DataItem?.GetType().Name}");
                    return;
                }

                Debug.WriteLine($"[VerRegistrosPopup] 📊 Registro selecionado: {registro.NumeroResposta}");

                // Feedback tátil imediato
                HapticHelper.VibrateClick();

                // Fecha o popup com o registro selecionado
                await CloseWithLogAsync(registro);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerRegistrosPopup] ❌ Erro ao processar item selecionado: {ex.Message}");
                
                // Recovery: tenta fechar o popup mesmo com erro
                try
                {
                    await CloseWithLogAsync(null!);
                }
                catch (Exception recoveryEx)
                {
                    Debug.WriteLine($"[VerRegistrosPopup] 🚨 Recovery falhou: {recoveryEx.Message}");
                }
            }
        }

        /// <summary>
        /// Override do método CloseAsync para incluir logging
        /// </summary>
        public async Task CloseWithLogAsync(LoteFormAvaliacaoGalpao? result)
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                Debug.WriteLine($"[VerRegistrosPopup] Fechando popup - Resultado: {(result?.NumeroResposta.ToString() ?? "NULL")}");
                
                // Chama o método base da classe Popup
                await base.CloseAsync(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerRegistrosPopup] ❌ Erro ao fechar popup: {ex.Message}");
            }
        }

        /// <summary>
        /// Método estático para facilitar o uso do popup
        /// </summary>
        /// <param name="registros">Lista de registros da avaliação</param>
        /// <param name="isQualitativo">Indica se é avaliação qualitativa</param>
        /// <returns>Registro selecionado ou null se cancelado</returns>
        public static async Task<LoteFormAvaliacaoGalpao?> ShowAsync(
            ObservableCollection<LoteFormAvaliacaoGalpao> registros,
            bool isQualitativo,
            int? registroAtualNumero = null)
        {
            try
            {
                Debug.WriteLine($"[VerRegistrosPopup] Abrindo popup - Registros: {registros?.Count ?? 0}, Qualitativo: {isQualitativo}");

                var popup = new VerRegistrosPopup(registros, isQualitativo, registroAtualNumero);
                var result = await NavigationUtils.ShowPopupAsync<LoteFormAvaliacaoGalpao>(popup);
                
                Debug.WriteLine($"[VerRegistrosPopup] Popup fechado - Resultado: {result?.NumeroResposta.ToString() ?? "NULL"}");
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerRegistrosPopup] ❌ Erro ao mostrar popup: {ex.Message}");
                return null;
            }
        }
    }
}
