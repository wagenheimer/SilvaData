using System;
using System.Threading.Tasks;

using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // MUDANÇA: Necessário para [RelayCommand]

using SilvaData.Models;

// Removido: using System.Windows.Input; (Substituído por CommunityToolkit.Mvvm.Input)

using SilvaData.Utilities;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// Popup para fechamento de lote com campos para data e observações.
    /// </summary>
    // MUDANÇA: A classe aninhada não precisa do 'static using' na própria classe
    public partial class PopUpFecharLote : Popup<LoteFechamentoInfo>
    {
        /// <summary>
        /// Inicializa uma nova instância do popup de fechamento de lote.
        /// </summary>
        /// <param name="titulo">Título do popup</param>
        /// <param name="mensagem">Mensagem explicativa</param>
        public PopUpFecharLote(string titulo, string mensagem)
        {
            InitializeComponent();
            BindingContext = new PopUpFecharLoteViewModel(this, titulo, mensagem);
        }

        /// <summary>
        /// Exibe um popup para fechamento de lote com campos para data e observações.
        /// </summary>
        /// <param name="titulo">Título do popup</param>
        /// <param name="mensagem">Mensagem explicativa</param>
        /// <returns>Informações do fechamento do lote (data, observações, confirmado)</returns>
        public static async Task<LoteFechamentoInfo> ShowAsync(string titulo, string mensagem)
        {
            if (string.IsNullOrEmpty(titulo))
                titulo = "Fechar Lote";

            var popup = new PopUpFecharLote(titulo, mensagem);

            // Usa o método genérico do NavigationUtils (migrado)
            var result = await NavigationUtils.ShowPopupAsync<LoteFechamentoInfo>(popup);

            // Garante que nunca retorne nulo, fornecendo valores padrão
            return result ?? LoteFechamentoInfo.Default();
        }
    }

    /// <summary>
    /// ViewModel para o popup de fechamento de lote.
    /// </summary>
    // MUDANÇA: A classe precisa ser 'partial' para o MVVM Toolkit
    public partial class PopUpFecharLoteViewModel : ObservableObject
    {
        // MUDANÇA: A referência ao PopUp usa a classe PopUpFecharLote
        private readonly PopUpFecharLote _popup;

        /// <summary>
        /// Título do popup.
        /// </summary>
        public string Titulo { get; }

        /// <summary>
        /// Mensagem explicativa.
        /// </summary>
        public string Mensagem { get; }

        /// <summary>
        /// Data de fechamento do lote.
        /// </summary>
        [ObservableProperty]
        private DateTime dataFechamento = DateTime.Now;

        /// <summary>
        /// Observações sobre o fechamento.
        /// </summary>
        [ObservableProperty]
        private string observacoes = string.Empty;

        // MUDANÇA: Comandos manuais ICommand removidos.

        /// <summary>
        /// Inicializa uma nova instância do ViewModel.
        /// </summary>
        public PopUpFecharLoteViewModel(PopUpFecharLote popup, string titulo, string mensagem)
        {
            _popup = popup ?? throw new ArgumentNullException(nameof(popup));
            Titulo = titulo ?? "Fechar Lote";
            Mensagem = mensagem ?? string.Empty;

            // MUDANÇA: Comandos não são mais inicializados manualmente
        }

        // MUDANÇA: Usando [RelayCommand] para o comando de Confirmação
        [RelayCommand]
        private Task ConfirmarAsync() // MUDANÇA: Método assíncrono que retorna Task
        {
            // Cria um objeto com as informações do fechamento
            var info = new LoteFechamentoInfo
            {
                DataFechamento = DataFechamento,
                Observacoes = Observacoes ?? string.Empty,
                Confirmado = true
            };

            // MUDANÇA: Fecha o popup usando CloseAsync (sempre a forma preferida)
            return _popup.CloseAsync(info);
        }

        // MUDANÇA: Usando [RelayCommand] para o comando de Cancelamento
        [RelayCommand]
        private Task CancelarAsync() // MUDANÇA: Método assíncrono que retorna Task
        {
            // Cria um objeto com as informações, mas com Confirmado = false
            var info = new LoteFechamentoInfo
            {
                // Mantemos a data e observações para consistência de dados
                DataFechamento = DataFechamento,
                Observacoes = Observacoes ?? string.Empty,
                Confirmado = false
            };

            // MUDANÇA: Fecha o popup usando CloseAsync (sempre a forma preferida)
            return _popup.CloseAsync(info);
        }
    }
}
