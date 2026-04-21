using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;

using SilvaData.Utils;

using SilvaData.Models;
using SilvaData.Utilities;

using System.Windows.Input;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// Popup para avaliação Net Promoter Score (NPS).
    /// </summary>
    public partial class PopUpNPS : Popup<NPSResult>
    {
        /// <summary>
        /// Inicializa uma nova instância do popup de avaliação NPS.
        /// </summary>
        /// <param name="titulo">Título do popup</param>
        /// <param name="mensagem">Mensagem explicativa sobre a avaliação</param>
        public PopUpNPS(string titulo, string mensagem)
        {
            InitializeComponent();
            BindingContext = new PopUpNPSViewModel(this, titulo, mensagem);
        }

        /// <summary>
        /// Exibe um popup de avaliação NPS e retorna o resultado da avaliação.
        /// </summary>
        /// <param name="titulo">Título do popup</param>
        /// <param name="mensagem">Mensagem explicativa sobre a avaliação</param>
        /// <returns>Resultado da avaliação (nota e comentários) ou valores padrão se cancelado</returns>
        public static async Task<NPSResult> ShowAsync(string titulo, string mensagem)
        {
            var popup = new PopUpNPS(titulo, mensagem);

            // Usa o método genérico do NavigationUtils que já lida com o tipo de retorno
            var result = await NavigationUtils.ShowPopupAsync<NPSResult>(popup);

            // Garante que nunca retorne nulo, mesmo se o usuário fechar o popup sem selecionar
            return result ?? NPSResult.Default();
        }
    }

    /// <summary>
    /// ViewModel para o popup de avaliação NPS.
    /// </summary>
    public partial class PopUpNPSViewModel : ObservableObject
    {
        private readonly PopUpNPS _popup;

        /// <summary>
        /// Título do popup.
        /// </summary>
        public string Titulo { get; }

        /// <summary>
        /// Mensagem explicativa sobre a avaliação.
        /// </summary>
        public string Mensagem { get; }

        /// <summary>
        /// Nota dada pelo usuário (0-10).
        /// </summary>
        [ObservableProperty]
        private double rating = 5;

        /// <summary>
        /// Comentários adicionais fornecidos pelo usuário.
        /// </summary>
        [ObservableProperty]
        private string comments = string.Empty;

        /// <summary>
        /// Comando para enviar a avaliação.
        /// </summary>
        public ICommand EnviarCommand { get; }

        /// <summary>
        /// Comando para cancelar a avaliação.
        /// </summary>
        public ICommand CancelarCommand { get; }

        /// <summary>
        /// Inicializa uma nova instância do ViewModel.
        /// </summary>
        /// <param name="popup">Referência para o popup</param>
        /// <param name="titulo">Título do popup</param>
        /// <param name="mensagem">Mensagem explicativa sobre a avaliação</param>
        public PopUpNPSViewModel(PopUpNPS popup, string titulo, string mensagem)
        {
            _popup = popup ?? throw new ArgumentNullException(nameof(popup));
            Titulo = titulo ?? "Avaliação";
            Mensagem = mensagem ?? "Avalie nossa solução";

            EnviarCommand = new Command(Enviar);
            CancelarCommand = new Command(Cancelar);
        }

        private void Enviar()
        {
            _popup.CloseAsync(new NPSResult
            {
                Rating = (int)Rating,
                Comments = Comments ?? string.Empty
            });
        }

        private void Cancelar()
        {
            _popup.CloseAsync();
        }

        public static bool JaDeuNotaNPS => (!string.IsNullOrEmpty(ISIWebService.Instance.LoggedUser.nps) && ISIWebService.Instance.LoggedUser.nps != "-1");

    }
}
