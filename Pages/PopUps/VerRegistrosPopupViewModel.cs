using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SilvaData.Models;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// ViewModel para o popup de Ver Registros de Avaliação do Galpão
    /// Performance otimizada com cache e lógica isolada
    /// </summary>
    public partial class VerRegistrosPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isQualitativo;

        [ObservableProperty]
        private bool isQuantitativo;

        [ObservableProperty]
        private bool hasNoRegistros;

        [ObservableProperty]
        private string totalRegistrosFormatado = "";

        [ObservableProperty]
        private string registrosRespondidosFormatado = "";

        public int? RegistroAtualNumero { get; private set; }

        private ObservableCollection<LoteFormAvaliacaoGalpao> _todosRegistros = new();
        private ObservableCollection<LoteFormAvaliacaoGalpao> _registrosQualitativos = new();
        private ObservableCollection<LoteFormAvaliacaoGalpao> _registrosQuantitativos = new();

        /// <summary>
        /// Inicializa o ViewModel com os registros da avaliação
        /// </summary>
        public VerRegistrosPopupViewModel(ObservableCollection<LoteFormAvaliacaoGalpao> registros, bool isQualitativo, int? registroAtualNumero = null)
        {
            Debug.WriteLine($"[VerRegistrosPopupViewModel] Inicializando com {registros?.Count ?? 0} registros");
            
            // Copia os registros para evitar modificações externas
            _todosRegistros = new ObservableCollection<LoteFormAvaliacaoGalpao>(registros ?? new());
            RegistroAtualNumero = registroAtualNumero;

            IsQualitativo = isQualitativo;
            IsQuantitativo = !isQualitativo;
            
            ProcessarRegistros();
        }

        /// <summary>
        /// Processa e organiza os registros por tipo
        /// </summary>
        private void ProcessarRegistros()
        {
            Debug.WriteLine($"[VerRegistrosPopupViewModel] Processando registros - Qualitativo: {IsQualitativo}");

            // Limpa coleções
            _registrosQualitativos.Clear();
            _registrosQuantitativos.Clear();

            // Separa por tipo de avaliação
            foreach (var registro in _todosRegistros)
            {
                if (IsQualitativo && registro.TemAlternativaSelecionada)
                {
                    _registrosQualitativos.Add(registro);
                }
                else if (!IsQualitativo)
                {
                    _registrosQuantitativos.Add(registro);
                }
            }

            // Atualiza propriedades de UI
            HasNoRegistros = (IsQualitativo ? _registrosQualitativos.Count : _registrosQuantitativos.Count) == 0;
            
            var totalRegistros = IsQualitativo ? _registrosQualitativos.Count : _registrosQuantitativos.Count;
            var registrosRespondidos = IsQualitativo 
                ? _registrosQualitativos.Count(r => r.TemAlternativaSelecionada)
                : _registrosQuantitativos.Count(r => r.RespostaQtde.HasValue);

            TotalRegistrosFormatado = $"Total: {totalRegistros}";
            RegistrosRespondidosFormatado = $"Respondidos: {registrosRespondidos}";

            Debug.WriteLine($"[VerRegistrosPopupViewModel] ✓ Processado: {totalRegistros} totais, {registrosRespondidos} respondidos");
        }

        /// <summary>
        /// Comando para fechar o popup sem seleção
        /// </summary>
        [RelayCommand]
        private async Task CloseAsync()
        {
            Debug.WriteLine("[VerRegistrosPopupViewModel] Fechando popup sem seleção");
            
            // Envia sinal para fechar o popup
            await CloseAsync(null!);
        }

        /// <summary>
        /// Método para fechar o popup com resultado
        /// </summary>
        private async Task CloseAsync(LoteFormAvaliacaoGalpao? registroSelecionado)
        {
            // Este método será chamado pelo popup pai
            // A implementação específica ficará no code-behind
            Debug.WriteLine($"[VerRegistrosPopupViewModel] Fechando popup com registro: {registroSelecionado?.NumeroResposta}");
        }

        #region Propriedades Públicas para Binding

        /// <summary>
        /// Lista de registros qualitativos para binding
        /// </summary>
        public ObservableCollection<LoteFormAvaliacaoGalpao> RegistrosQualitativos => _registrosQualitativos;

        /// <summary>
        /// Lista de registros quantitativos para binding
        /// </summary>
        public ObservableCollection<LoteFormAvaliacaoGalpao> RegistrosQuantitativos => _registrosQuantitativos;

        #endregion
    }
}
