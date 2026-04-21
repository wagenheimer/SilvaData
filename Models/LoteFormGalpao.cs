using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Newtonsoft.Json;
using SQLite;
using System.Diagnostics;

namespace SilvaData.Models
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // CLASSES DO WEB SERVICE (DTOs para comunicação com API)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// DTO: Dados de avaliação do galpão para envio ao web service.
    /// </summary>
    public class LoteFormParametroGalpaoDoWebService
    {
        /// <summary>Tipo: 7 = Qualitativo, 8 = Quantitativo</summary>
        public string? tipoPreenchimento { get; set; }

        /// <summary>Número sequencial da resposta (1, 2, 3...)</summary>
        public int? numeroResposta { get; set; }

        /// <summary>Valor da resposta quantitativa (ex: 2500g)</summary>
        public int? respostaQtde { get; set; }

        /// <summary>ID da alternativa selecionada (qualitativo)</summary>
        public int? alternativaId { get; set; }
    }

    /// <summary>
    /// DTO: Parâmetros do galpão para web service.
    /// </summary>
    public class ParametrosGalpaoDoWebService : ObservableObject
    {
        public string? item { get; set; }
        public string? indice { get; set; }
        public string? valor { get; set; }
        public string? parametroAlternativa { get; set; }
    }

    /// <summary>
    /// DTO: Parâmetro do web service (simplificado).
    /// </summary>
    public class LoteFormParametrosdoWebService
    {
        public int? id { get; set; }
    }

    /// <summary>
    /// DTO: Dados completos do galpão para web service.
    /// </summary>
    public class LoteFormGalpaoWebService
    {
        public List<LoteFormImagem>? imagens { get; set; }
        public int? loteVisita { get; set; }
        public int? parametroTipoId { get; set; }
        public string? observacoes { get; set; }
        public DateTime dataUltimaAtualizacao { get; set; }
        public int? item { get; set; }
        public DateTime data { get; set; }
        public List<ParametrosGalpaoGalpaoWebService>? parametrosGalpao { get; set; }
        public int? loteFormFaseId { get; set; }
        public int? id { get; set; }
        public string? excluido { get; set; }
        public List<ParametroGalpaoWebService>? parametros { get; set; }
        public int? loteId { get; set; }
    }

    /// <summary>
    /// DTO: Parâmetro do galpão para web service.
    /// </summary>
    public class ParametroGalpaoWebService
    {
        public string? qtdCampos { get; set; }
        public object? campoTipo { get; set; }
        public string? qtdMinima { get; set; }
        public int? parametroId { get; set; }
        public string? valor { get; set; }
    }

    /// <summary>
    /// DTO: Parâmetros do galpão (detalhado) para web service.
    /// </summary>
    public class ParametrosGalpaoGalpaoWebService
    {
        public int? indice { get; set; }
        public int? item { get; set; }
        public string? parametroAlternativa { get; set; }
        public int? valor { get; set; }
    }

    /// <summary>
    /// DTO: Resultado de consulta de avaliações do galpão.
    /// </summary>
    public class ResultadogetLoteFormsGalpao
    {
        public List<LoteFormGalpaoWebService>? loteForms { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // MODELO PRINCIPAL: LOTE FORM AVALIAÇÃO GALPÃO
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ★★★ MODELO DE AVALIAÇÃO DO GALPÃO ★★★
    /// Representa uma única resposta de avaliação (Qualitativa ou Quantitativa).
    /// 
    /// TIPOS DE AVALIAÇÃO:
    /// - Quantitativa: RespostaQtde preenchido (ex: Peso = 2500g)
    /// - Qualitativa (Única): AlternativaIds com 1 elemento
    /// - Qualitativa (Múltipla): AlternativaIds com N elementos
    /// 
    /// RECÁLCULO AUTOMÁTICO:
    /// ✅ Quando RespostaQtde muda → Dispara RecalcularAvaliacaoGalpaoMessage
    /// ✅ Quando AlternativaIds muda → Dispara RecalcularAvaliacaoGalpaoMessage
    /// ✅ LoteFormularioViewModel recalcula totais e média INSTANTANEAMENTE
    /// </summary>
    public partial class LoteFormAvaliacaoGalpao : ObservableObject
    {
        /// <summary>
        /// Flag global para evitar que a "tremidinha" dispare durante o carregamento inicial do banco de dados.
        /// Deve ser controlada pelo ViewModel responsável pelo carregamento.
        /// </summary>
        [Ignore]
        public static bool IsLoadingData { get; set; } = false;

        #region Propriedades do Banco de Dados (SQLite)

        /// <summary>
        /// ID do parâmetro associado (FK para Parametro)
        /// Ex: 123 = Peso, 124 = Altura
        /// ✅ Indexed para performance em queries
        /// </summary>
        [Indexed]
        public int? parametroId { get; set; }

        /// <summary>
        /// ID do formulário do lote (FK para LoteForm)
        /// Null quando o formulário ainda não foi salvo
        /// ✅ Indexed para queries por formulário
        /// </summary>
        [Indexed]
        public int? LoteFormId { get; set; }

        /// <summary>
        /// ★★★ Número sequencial do registro (1, 2, 3...) ★★★
        /// Identifica cada avaliação individual dentro do mesmo parâmetro
        /// Ex: Avaliação de Peso #1, #2, #3 (quando qtdMinima = 3)
        /// ✅ CORRIGIDO: Propriedade pública auto-implementada (não usa ObservableProperty)
        /// </summary>
        [Indexed]
        public int? NumeroResposta { get; set; }

        #endregion

        #region Propriedades Observáveis (Com notificação de mudança)

        /// <summary>
        /// ★★★ RESPOSTA QUANTITATIVA (valor numérico) ★★★
        /// Ex: Peso = 2500g, Altura = 35cm, Temperatura = 28°C
        /// Null quando não preenchido ou quando é avaliação qualitativa
        /// 
        /// IMPORTANTE: Dispara recálculo automático ao mudar (OnRespostaQtdeChanged)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CorFundo))]
        [NotifyPropertyChangedFor(nameof(TemAlternativaSelecionada))]
        private int? respostaQtde;

        /// <summary>
        /// ★★★ RESPOSTA QUALITATIVA (JSON com IDs das alternativas) ★★★
        /// Formato JSON: "[1, 3, 5]" para múltipla escolha
        /// Formato JSON: "[2]" para escolha única
        /// Null/empty quando não preenchido ou quando é avaliação quantitativa
        /// 
        /// IMPORTANTE: Dispara recálculo automático ao mudar (OnAlternativaIdsJsonChanged)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AlternativaIds))]
        [NotifyPropertyChangedFor(nameof(TemAlternativaSelecionada))]
        [NotifyPropertyChangedFor(nameof(TemAlternativaSelecionadaAindaNaoConfirmada))]
        [NotifyPropertyChangedFor(nameof(ResumoSelecionadas))]
        [NotifyPropertyChangedFor(nameof(CorFundo))]
        private string? alternativaIdsJson;

        #endregion

        #region Propriedades de Navegação (Não persistidas - apenas memória)

        /// <summary>
        /// Objeto Parametro completo (carregado do banco)
        /// Contém: nome, valorMinimo, valorMaximo, campoTipo, etc.
        /// </summary>
        [ObservableProperty]
        [property: Ignore]
        [NotifyPropertyChangedFor(nameof(ParametroValorMinimo))]
        [NotifyPropertyChangedFor(nameof(ParametroValorMaximo))]
        private Parametro? parametro;

        /// <summary>
        /// ★★★ CRÍTICO: Lista de alternativas possíveis para este parâmetro ★★★
        /// Usado no binding do DataTemplate Qualitativo (ComboBox/CollectionView)
        /// Carregada de ParametroAlternativas.PegaAlternativas(parametroId)
        /// </summary>
        [ObservableProperty]
        [property: Ignore]
        private ObservableCollection<ParametroAlternativas> parametroAlternativas = new();

        /// <summary>
        /// Flag temporária: usuário escolheu mas não confirmou ainda
        /// Usado em modais de seleção qualitativa
        /// </summary>
        [ObservableProperty]
        [property: Ignore]
        private bool temAlternativaSelecionadaAindaNaoConfirmada;

        #endregion

        #region Propriedades Computadas (Calculadas em tempo real)

        /// <summary>
        /// Texto formatado do número da resposta
        /// Ex: "Registro 1", "Registro 2"
        /// Usado no header do card
        /// </summary>
        [Ignore]
        public string NumeroRespostaFormatado => NumeroResposta.HasValue
            ? $"{Traducao.Registro} {NumeroResposta}"
            : string.Empty;

        /// <summary>
        /// ★★★ COR DE FUNDO DINÂMICA ★★★
        /// Verde claro (#F0FBF5): Preenchido (tem resposta)
        /// Vermelho claro (#FFF2F0): Vazio (sem resposta)
        /// 
        /// Atualiza automaticamente quando RespostaQtde ou AlternativaIds muda
        /// </summary>
        [Ignore]
        public Color CorFundo => RespostaQtde.HasValue || AlternativaIds.Any()
            ? Color.FromArgb("#F0FBF5") // Verde claro = Preenchido ✅
            : Color.FromArgb("#FFF2F0"); // Vermelho claro = Vazio ❌

        /// <summary>
        /// Valor mínimo permitido para validação (vem do Parametro)
        /// Ex: Peso mínimo = 2000g
        /// </summary>
        [Ignore]
        public float ParametroValorMinimo => Parametro?.valorMinimo ?? 0;

        /// <summary>
        /// Valor máximo permitido para validação (vem do Parametro)
        /// Ex: Peso máximo = 3000g
        /// </summary>
        [Ignore]
        public float ParametroValorMaximo => Parametro?.valorMaximo ?? 100;

        /// <summary>
        /// ★★★ Lista de IDs das alternativas selecionadas ★★★
        /// Converte JSON string para List de inteiros automaticamente
        /// Ex: "[1, 3, 5]" - List of IDs
        /// </summary>
        [Ignore]
        [JsonIgnore]
        public List<int> AlternativaIds
        {
            get => string.IsNullOrEmpty(AlternativaIdsJson)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(AlternativaIdsJson) ?? new List<int>();
            set
            {
                var newValue = JsonConvert.SerializeObject(value ?? new List<int>());
                if (AlternativaIdsJson != newValue)
                {
                    AlternativaIdsJson = newValue;
                    OnPropertyChanged(nameof(AlternativaIds));
                    OnPropertyChanged(nameof(ResumoSelecionadas));
                    OnPropertyChanged(nameof(TemAlternativaSelecionada));
                    OnPropertyChanged(nameof(CorFundo));
                }
            }
        }

        /// <summary>
        /// ★ Resumo textual das alternativas selecionadas
        /// Ex: "Opção A, Opção C, Opção E"
        /// Usado para exibir seleção em modo compacto
        /// </summary>
        [Ignore]
        public string ResumoSelecionadas
        {
            get
            {
                if (AlternativaIds == null || !AlternativaIds.Any() || ParametroAlternativas == null)
                    return string.Empty;

                var descricoes = AlternativaIds
                    .Select(id => ParametroAlternativas
                        .FirstOrDefault(a => a.id == id)?.descricao ?? string.Empty)
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList();

                return string.Join(", ", descricoes);
            }
        }

        /// <summary>
        /// ★★★ VALIDAÇÃO: Tem resposta (Qualitativa OU Quantitativa) ★★★
        /// True = Campo preenchido (pode salvar)
        /// False = Campo vazio (falta preencher)
        /// </summary>
        [Ignore]
        public bool TemAlternativaSelecionada =>
            (AlternativaIds?.Count > 0) || RespostaQtde.HasValue;

        #endregion

        #region Métodos Estáticos (Database Operations)

        /// <summary>
        /// ★ Recupera lista de parâmetros do tipo 20 (Avaliação do Galpão).
        /// Usado no ComboBox de seleção de parâmetro.
        /// </summary>
        public static async Task<List<Parametro>> ListaParametrosAvalicaoGalpao()
        {
            var table = await Db.Table<Parametro>();
            return await table
                .Where(p => p.parametroTipo == 20 
                    && p.exibir == 1
                    && (p.excluido == null || p.excluido != 1))
                .ToListAsync();
        }

        /// <summary>
        /// ★ Recupera todas as avaliações de um formulário específico.
        /// Retorna lista ordenada por NumeroResposta.
        /// </summary>
        public static async Task<List<LoteFormAvaliacaoGalpao>> RespostasAvaliacaoGalpaoPorLote(int? loteFormId)
        {
            if (!loteFormId.HasValue || loteFormId.Value <= 0)
                return new List<LoteFormAvaliacaoGalpao>();

            var table = await Db.Table<LoteFormAvaliacaoGalpao>();
            var resultados = await table
                .Where(l => l.LoteFormId == loteFormId)
                .OrderBy(l => l.NumeroResposta)
                .ToListAsync();

            Debug.WriteLine($"[LoteFormAvaliacaoGalpao] RespostasAvaliacaoGalpaoPorLote({loteFormId}): {resultados.Count} registros");

            return resultados;
        }

        /// <summary>
        /// ★ Salva ou atualiza avaliação no banco.
        /// Insert se não tem LoteFormId, Update se já existe.
        /// </summary>
        public async Task<bool> SalvarAsync()
        {
            try
            {
                if (LoteFormId.HasValue && LoteFormId.Value > 0)
                {
                    await Db.UpdateAsync(this);
                    Debug.WriteLine($"[LoteFormAvaliacaoGalpao] ✅ Atualizado: NumeroResposta={NumeroResposta}");
                }
                else
                {
                    await Db.InsertAsync(this);
                    Debug.WriteLine($"[LoteFormAvaliacaoGalpao] ✅ Inserido: NumeroResposta={NumeroResposta}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteFormAvaliacaoGalpao] ❌ Erro ao salvar: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Eventos e Notificações (Property Changed Handlers)

        /// <summary>
        /// ★★★ DISPARADO AUTOMATICAMENTE quando RespostaQtde muda ★★★
        /// FLUXO: Usuário digita valor → Binding TwoWay → Este método → Recálculo instantâneo
        /// </summary>
        partial void OnRespostaQtdeChanged(int? oldValue, int? newValue)
        {
            Debug.WriteLine($"[LoteFormAvaliacaoGalpao] ★ RespostaQtde mudou: {oldValue} → {newValue}");

            // ✅ Tremidinha (Haptic feedback) ao mudar o valor
            // Evita disparar durante o carregamento do banco de dados (IsLoadingData)
            if (oldValue != newValue && !IsLoadingData)
            {
                HapticHelper.VibrateClick();
            }

            // Clamp valor digitado manualmente para o range permitido
            if (newValue.HasValue)
            {
                var min = (int)Math.Round(ParametroValorMinimo);
                var max = (int)Math.Round(ParametroValorMaximo);
                var clamped = Math.Clamp(newValue.Value, min, max);
                if (clamped != newValue.Value)
                {
                    RespostaQtde = clamped;
                    return;
                }
            }

            // ✅ Atualiza propriedades locais (cor, validação)
            OnPropertyChanged(nameof(CorFundo));
            OnPropertyChanged(nameof(TemAlternativaSelecionada));

            // ★★★ DISPARA RECÁLCULO INSTANTÂNEO de Respondidos e Média ★★★
            WeakReferenceMessenger.Default.Send(new RecalcularAvaliacaoGalpaoMessage());
        }

        /// <summary>
        /// ★★★ DISPARADO quando alternativas mudam (Qualitativo) ★★★
        /// FLUXO: Usuário seleciona alternativa → AlternativaIds.set → Este método → Recálculo
        /// </summary>
        partial void OnAlternativaIdsJsonChanged(string? oldValue, string? newValue)
        {
            Debug.WriteLine($"[LoteFormAvaliacaoGalpao] ★ AlternativaIdsJson mudou");

            // ✅ Atualiza propriedades dependentes
            OnPropertyChanged(nameof(CorFundo));
            OnPropertyChanged(nameof(TemAlternativaSelecionada));
            OnPropertyChanged(nameof(ResumoSelecionadas));
            OnPropertyChanged(nameof(AlternativaIds));

            // ★★★ DISPARA RECÁLCULO INSTANTÂNEO ★★★
            WeakReferenceMessenger.Default.Send(new RecalcularAvaliacaoGalpaoMessage());
        }

        [RelayCommand]
        private void AumentarQtde()
        {
            HapticHelper.VibrateClick();
            var min = (int)Math.Round(ParametroValorMinimo);
            var max = (int)Math.Round(ParametroValorMaximo);
            var valorAtual = RespostaQtde ?? min;
            var proximo = valorAtual + 1;

            RespostaQtde = proximo > max ? max : proximo;
        }

        [RelayCommand]
        private void DiminuirQtde()
        {
            HapticHelper.VibrateClick();
            var min = (int)Math.Round(ParametroValorMinimo);
            var max = (int)Math.Round(ParametroValorMaximo);
            var valorAtual = RespostaQtde ?? min;
            var anterior = valorAtual - 1;

            RespostaQtde = anterior < min ? min : (anterior > max ? max : anterior);
        }

        #endregion

        #region Métodos Públicos (Ações do Usuário)

        /// <summary>
        /// ★ Notifica que o usuário escolheu resposta mas ainda não confirmou.
        /// Usado em modais de seleção antes de confirmar escolha.
        /// </summary>
        public void EscolheuRespostaMasAindaNaoConfirmou()
        {
            TemAlternativaSelecionadaAindaNaoConfirmada = true;

            OnPropertyChanged(nameof(TemAlternativaSelecionada));
            OnPropertyChanged(nameof(TemAlternativaSelecionadaAindaNaoConfirmada));

            // ✅ Dispara recálculo
            WeakReferenceMessenger.Default.Send(new RecalcularAvaliacaoGalpaoMessage());
        }

        /// <summary>
        /// ★ Confirma a resposta escolhida (fecha modal).
        /// </summary>
        public void ConfirmarResposta()
        {
            TemAlternativaSelecionadaAindaNaoConfirmada = false;

            OnPropertyChanged(nameof(TemAlternativaSelecionada));

            // ✅ Dispara recálculo final
            WeakReferenceMessenger.Default.Send(new RecalcularAvaliacaoGalpaoMessage());
        }

        #endregion

        #region Métodos de Validação

        /// <summary>
        /// ★ Valida se o registro está preenchido corretamente.
        /// QUANTITATIVO: Valor dentro do range [min, max]
        /// QUALITATIVO: Pelo menos 1 alternativa selecionada
        /// </summary>
        public bool EstaValido()
        {
            // Validação QUANTITATIVA
            if (RespostaQtde.HasValue)
            {
                var valor = RespostaQtde.Value;
                var min = ParametroValorMinimo;
                var max = ParametroValorMaximo;

                if (valor < min || valor > max)
                {
                    Debug.WriteLine($"[LoteFormAvaliacaoGalpao] ❌ Valor {valor} fora do range [{min}, {max}]");
                    return false;
                }

                return true;
            }

            // Validação QUALITATIVA
            if (AlternativaIds?.Any() == true)
            {
                return true;
            }

            // Sem resposta
            Debug.WriteLine($"[LoteFormAvaliacaoGalpao] ⚠️ Registro {NumeroResposta} sem resposta");
            return false;
        }

        /// <summary>
        /// ★ Limpa todas as respostas (reset do campo).
        /// </summary>
        public void Limpar()
        {
            RespostaQtde = null;
            AlternativaIds = new List<int>();
            TemAlternativaSelecionadaAindaNaoConfirmada = false;

            Debug.WriteLine($"[LoteFormAvaliacaoGalpao] 🧹 Registro {NumeroResposta} limpo");
        }

        #endregion

        #region Sobrescritas (Debug e Logging)

        /// <summary>
        /// ToString para debug - Facilita identificação nos logs
        /// </summary>
        public override string ToString()
        {
            var tipo = RespostaQtde.HasValue ? "Quantitativo" : "Qualitativo";
            var valor = RespostaQtde.HasValue
                ? $"Valor={RespostaQtde}"
                : $"Alternativas={AlternativaIds.Count}";

            return $"[Avaliação #{NumeroResposta}] {tipo} | {valor} | Válido={EstaValido()}";
        }

        #endregion
    }
}
