using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Controls;
using SilvaData.Pages.PopUps;


using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para seleção de alternativas em avaliações do galpão.
    /// MIGRADO PARA MAUI: Removido estático, usa Messenger.
    /// </summary>
    public partial class AvaliacaoAlternativasViewModel : ViewModelBase
    {
        // ★ FLAG ADICIONADA: Impede conflito de toque entre a foto e a linha
        private bool _bloquearSelecaoPorToqueNaFoto = false;

        [ObservableProperty]
        private bool podeEditar = true;

        [ObservableProperty]
        private int totalLiberado = 1;

        [ObservableProperty]
        private bool podeSelecionarMaisQueUm = false;

        [ObservableProperty]
        private LoteFormAvaliacaoGalpao? respostaSelecionada;

        [ObservableProperty]
        private ObservableCollection<ParametroAlternativas> alternativasParametroSelecionado = new();

        [ObservableProperty]
        private ObservableCollection<LoteFormAvaliacaoGalpao> listaAvaliacoesGalpao = new();

        [ObservableProperty]
        private ObservableCollection<LoteFormAvaliacaoGalpao> listaAvaliacoesGalpaoComboBox = new();

        [ObservableProperty]
        private int quantidadeMaximaRegistros;

        /// <summary>
        /// Construtor - registra listener de mensagem.
        /// </summary>
        public AvaliacaoAlternativasViewModel()
        {
            Debug.WriteLine($"[AvaliacaoAlternativasViewModel] ★ Construtor chamado - HashCode: {GetHashCode()}");

            WeakReferenceMessenger.Default.Register<SelecionouAvaliacaoQualitativaMessage>(this, (r, m) =>
            {
                RespostaSelecionada = m.Avaliacao;
                SelecionaAvaliacao();
            });
        }

        /// <summary>
        /// Cleanup - desregistra listener.
        /// </summary>
        public new void Cleanup()
        {
            WeakReferenceMessenger.Default.Unregister<SelecionouAvaliacaoQualitativaMessage>(this);
        }

        /// <summary>
        /// Quando as alternativas são alteradas.
        /// </summary>
        partial void OnAlternativasParametroSelecionadoChanged(ObservableCollection<ParametroAlternativas> value)
        {
            AtualizaTotalLiberado();
            AtualizaComboBoxLista();
            SelecionaAvaliacao();
        }

        /// <summary>
        /// Atualiza o total de respostas liberadas.
        /// </summary>
        public void AtualizaTotalLiberado()
        {
            TotalLiberado = ListaAvaliacoesGalpao.Count(a => a.AlternativaIds.Count > 0);
            if (TotalLiberado == 0)
                TotalLiberado = 1; // Inserindo
        }

        /// <summary>
        /// Quando a lista de avaliações muda.
        /// </summary>
        partial void OnListaAvaliacoesGalpaoChanged(ObservableCollection<LoteFormAvaliacaoGalpao> value)
        {
            AtualizaComboBoxLista();
        }

        /// <summary>
        /// Comando para visualizar foto da alternativa.
        /// ★ ATUALIZADO: Levanta a flag de bloqueio para não selecionar a alternativa.
        /// </summary>
        [RelayCommand]
        private async Task VerFoto(ParametroAlternativas? alternativa)
        {
            try
            {
                // Levanta a flag ANTES de processar
                _bloquearSelecaoPorToqueNaFoto = true;

                Debug.WriteLine($"[AvaliacaoAlternativasViewModel] ★★★ VerFoto CHAMADO! ★★★");
                Debug.WriteLine($"  Alternativa: {alternativa?.descricao ?? "NULL"}");
                Debug.WriteLine($"  URL: {alternativa?.urlImagemLocal ?? "NULL"}");

                if (alternativa != null && !string.IsNullOrEmpty(alternativa.urlImagemLocal))
                {
                    Debug.WriteLine($"[AvaliacaoAlternativasViewModel] ★ Abrindo PhotoPopup...");
                    await PhotoPopup.ShowAsync(alternativa.urlImagemLocal, alternativa.descricao);
                }
                else
                {
                    Debug.WriteLine($"[AvaliacaoAlternativasViewModel] ⚠️ Alternativa nula ou sem URL!");
                }
            }
            finally
            {
                // Aguarda o tempo necessário para o toque da linha (ItemTapped) passar batido
                await Task.Delay(300);
                _bloquearSelecaoPorToqueNaFoto = false;
            }
        }

        /// <summary>
        /// Comando para selecionar uma avaliação.
        /// </summary>
        [RelayCommand]
        public void SelecionaAvaliacao()
        {
            AtualizaRespostas();
            PodeEditar = false;
        }

        /// <summary>
        /// Atualiza as respostas selecionadas.
        /// </summary>
        public void AtualizaRespostas()
        {
            if (RespostaSelecionada == null) return;

            foreach (var parametro in AlternativasParametroSelecionado)
                parametro.IsSelected = RespostaSelecionada.AlternativaIds.Contains(parametro.id);
        }

        /// <summary>
        /// Comando quando um frame é tocado (seleção de alternativa).
        /// ★ ATUALIZADO: Ignora a seleção se o usuário tocou na foto.
        /// </summary>
        [RelayCommand]
        public void FrameTapped(ParametroAlternativas? parametroAlternativas)
        {
            // Verifica a flag anti-conflito
            if (_bloquearSelecaoPorToqueNaFoto)
            {
                Debug.WriteLine($"[AvaliacaoAlternativasViewModel] ⚠️ Toque ignorado (usuário clicou na foto).");
                return;
            }

            Debug.WriteLine($"[AvaliacaoAlternativasViewModel] ★★★ FrameTapped CHAMADO! ★★★");
            Debug.WriteLine($"  ViewModel HashCode: {GetHashCode()}");
            Debug.WriteLine($"  Parametro: {parametroAlternativas?.descricao ?? "NULL"}");
            Debug.WriteLine($"  PodeEditar: {PodeEditar}");
            Debug.WriteLine($"  PodeSelecionarMaisQueUm: {PodeSelecionarMaisQueUm}");

            if (parametroAlternativas == null)
            {
                Debug.WriteLine($"[AvaliacaoAlternativasViewModel] ⚠️ Parametro NULL!");
                return;
            }

            // Se não pode selecionar múltiplas, desseleciona todos antes
            if (!PodeSelecionarMaisQueUm)
            {
                bool oldValue = parametroAlternativas.IsSelected;
                Debug.WriteLine($"[AvaliacaoAlternativasViewModel] Desmarcando todos... oldValue={oldValue}");

                foreach (var parametro in AlternativasParametroSelecionado)
                    parametro.IsSelected = false;

                parametroAlternativas.IsSelected = !oldValue;
                Debug.WriteLine($"[AvaliacaoAlternativasViewModel] Nova seleção: {parametroAlternativas.IsSelected}");
            }
            else
            {
                // Se pode selecionar múltiplas, alterna a seleção
                parametroAlternativas.IsSelected = !parametroAlternativas.IsSelected;
                Debug.WriteLine($"[AvaliacaoAlternativasViewModel] Toggle seleção: {parametroAlternativas.IsSelected}");
            }

            if (RespostaSelecionada != null)
            {
                RespostaSelecionada.TemAlternativaSelecionadaAindaNaoConfirmada = true;
                RespostaSelecionada.EscolheuRespostaMasAindaNaoConfirmou();
            }

            Debug.WriteLine($"[AvaliacaoAlternativasViewModel] ★ FrameTapped CONCLUÍDO");
        }

        /// <summary>
        /// Última avaliação da lista.
        /// </summary>
        public LoteFormAvaliacaoGalpao? UltimaAvaliacao =>
            TotalLiberado > 0 && TotalLiberado <= ListaAvaliacoesGalpao.Count
                ? ListaAvaliacoesGalpao[TotalLiberado - 1]
                : null;

        /// <summary>
        /// Verifica se a última avaliação tem alternativa selecionada.
        /// </summary>
        public bool UltimaAvaliacaoTemAlternativaSelecionada =>
            UltimaAvaliacao != null && UltimaAvaliacao.TemAlternativaSelecionada;

        /// <summary>
        /// Comando para adicionar nova avaliação.
        /// </summary>
        [RelayCommand]
        public async Task AdicionaNovo()
        {
            HapticHelper.VibrateClick();

            if (TotalLiberado >= QuantidadeMaximaRegistros && QuantidadeMaximaRegistros > 0)
            {
                await PopUpOK.ShowAsync(
                    Traducao.Erro,
                    string.Format(Traducao.JaFoiMaximoRespostas, QuantidadeMaximaRegistros));
                return;
            }

            if (UltimaAvaliacaoTemAlternativaSelecionada)
            {
                TotalLiberado++;
                AtualizaComboBoxLista();
            }
            else
            {
                RespostaSelecionada = UltimaAvaliacao;
            }

            PodeEditar = true;
            AtualizaRespostas();

            if (RespostaSelecionada != null)
                RespostaSelecionada.TemAlternativaSelecionadaAindaNaoConfirmada = false;
        }

        /// <summary>
        /// Comando para confirmar resposta.
        /// ✅ MUDANÇA: Usa Messenger em vez de estático
        /// </summary>
        [RelayCommand]
        public void ConfirmarResposta()
        {
            if (RespostaSelecionada == null) return;

            var alterando = RespostaSelecionada.TemAlternativaSelecionada;

            // Atualiza as alternativas selecionadas
            RespostaSelecionada.AlternativaIds = AlternativasParametroSelecionado
                .Where(x => x.IsSelected)
                .Select(x => x.id)
                .ToList();

            // Envia mensagem de mudança
            WeakReferenceMessenger.Default.Send(
                new PropriedadeMudouMessage(nameof(RespostaSelecionada.AlternativaIds), null, null));

            PodeEditar = false;
            RespostaSelecionada.TemAlternativaSelecionadaAindaNaoConfirmada = false;

            HapticHelper.VibrateClick();
            // ✅ Envia mensagem para salvar em andamento
            WeakReferenceMessenger.Default.Send(new UpdateScoreMessage());

            if (!alterando)
                _ = AdicionaNovo();
        }

        /// <summary>
        /// Comando para editar resposta.
        /// </summary>
        [RelayCommand]
        public void Editar()
        {
            PodeEditar = true;

            if (RespostaSelecionada != null)
                RespostaSelecionada.TemAlternativaSelecionadaAindaNaoConfirmada = true;
        }

        /// <summary>
        /// Atualiza a lista de avaliações do ComboBox.
        /// </summary>
        public void AtualizaComboBoxLista()
        {
            ListaAvaliacoesGalpaoComboBox.Clear();

            foreach (var avaliacao in ListaAvaliacoesGalpao.Take(TotalLiberado))
                ListaAvaliacoesGalpaoComboBox.Add(avaliacao);

            if (ListaAvaliacoesGalpaoComboBox.Count > 0)
            {
                RespostaSelecionada = ListaAvaliacoesGalpaoComboBox.Last();
                OnPropertyChanged(nameof(RespostaSelecionada));
            }
        }

        /// <summary>
        /// Configura a quantidade máxima de registros permitidos.
        /// </summary>
        public void ConfigurarQuantidadeMaxima(int quantidadeMaxima)
        {
            QuantidadeMaximaRegistros = quantidadeMaxima;
            Debug.WriteLine($"[AvaliacaoAlternativasViewModel] Quantidade máxima configurada: {quantidadeMaxima}");
        }

        /// <summary>
        /// Reseta o ViewModel para estado inicial.
        /// </summary>
        public void Reset()
        {
            PodeEditar = true;
            TotalLiberado = 1;
            PodeSelecionarMaisQueUm = false;
            RespostaSelecionada = null;
            AlternativasParametroSelecionado.Clear();
            ListaAvaliacoesGalpao.Clear();
            ListaAvaliacoesGalpaoComboBox.Clear();
            QuantidadeMaximaRegistros = 0;
        }
    }
}
