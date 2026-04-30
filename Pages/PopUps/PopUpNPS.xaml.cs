using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;

using SilvaData.Utils;

using SilvaData.Models;
using SilvaData.Utilities;

using System.Windows.Input;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// Popup para avaliaï¿½ï¿½o Net Promoter Score (NPS).
    /// </summary>
    public partial class PopUpNPS : Popup<NPSResult>
    {
        /// <summary>
        /// Inicializa uma nova instï¿½ncia do popup de avaliaï¿½ï¿½o NPS.
        /// </summary>
        /// <param name="titulo">Tï¿½tulo do popup</param>
        /// <param name="mensagem">Mensagem explicativa sobre a avaliaï¿½ï¿½o</param>
        public PopUpNPS(string titulo, string mensagem)
        {
            InitializeComponent();
            BindingContext = new PopUpNPSViewModel(this, titulo, mensagem);
        }

        /// <summary>
        /// Exibe um popup de avaliaï¿½ï¿½o NPS e retorna o resultado da avaliaï¿½ï¿½o.
        /// </summary>
        /// <param name="titulo">Tï¿½tulo do popup</param>
        /// <param name="mensagem">Mensagem explicativa sobre a avaliaï¿½ï¿½o</param>
        /// <returns>Resultado da avaliaï¿½ï¿½o (nota e comentï¿½rios) ou valores padrï¿½o se cancelado</returns>
        public static async Task<NPSResult> ShowAsync(string titulo, string mensagem)
        {
            var popup = new PopUpNPS(titulo, mensagem);

            // Usa o mï¿½todo genï¿½rico do NavigationUtils que jï¿½ lida com o tipo de retorno
            var result = await NavigationUtils.ShowPopupAsync<NPSResult>(popup);

            // Garante que nunca retorne nulo, mesmo se o usuï¿½rio fechar o popup sem selecionar
            return result ?? NPSResult.Default();
        }
    }

    /// <summary>
    /// ViewModel para o popup de avaliaï¿½ï¿½o NPS.
    /// </summary>
    public partial class PopUpNPSViewModel : ObservableObject
    {
        private readonly PopUpNPS _popup;
        private bool _isClosing;

        /// <summary>
        /// Tï¿½tulo do popup.
        /// </summary>
        public string Titulo { get; }

        /// <summary>
        /// Mensagem explicativa sobre a avaliaï¿½ï¿½o.
        /// </summary>
        public string Mensagem { get; }

        /// <summary>
        /// Nota dada pelo usuï¿½rio (0-10).
        /// </summary>
        [ObservableProperty]
        private double rating = 5;

        /// <summary>
        /// Comentï¿½rios adicionais fornecidos pelo usuï¿½rio.
        /// </summary>
        [ObservableProperty]
        private string comments = string.Empty;

        /// <summary>
        /// Comando para enviar a avaliaï¿½ï¿½o.
        /// </summary>
        public ICommand EnviarCommand { get; }

        /// <summary>
        /// Comando para cancelar a avaliaï¿½ï¿½o.
        /// </summary>
        public ICommand CancelarCommand { get; }

        /// <summary>
        /// Inicializa uma nova instï¿½ncia do ViewModel.
        /// </summary>
        /// <param name="popup">Referï¿½ncia para o popup</param>
        /// <param name="titulo">Tï¿½tulo do popup</param>
        /// <param name="mensagem">Mensagem explicativa sobre a avaliaï¿½ï¿½o</param>
        public PopUpNPSViewModel(PopUpNPS popup, string titulo, string mensagem)
        {
            _popup = popup ?? throw new ArgumentNullException(nameof(popup));
            Titulo = titulo ?? "Avaliaï¿½ï¿½o";
            Mensagem = mensagem ?? "Avalie nossa soluï¿½ï¿½o";

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

        private async void `Cancelar() { if (_isClosing) return; _isClosing = true; try { await _popup.CloseAsync(`); } catch { } }

        public static bool JaDeuNotaNPS => (!string.IsNullOrEmpty(ISIWebService.Instance.LoggedUser.nps) && ISIWebService.Instance.LoggedUser.nps != "-1");

    }
}
