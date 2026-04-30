using CommunityToolkit.Maui.Views;
using System.Windows.Input;

namespace SilvaData.Pages.PopUps
{
    public partial class PopUpPrivacy : Popup<Boolean>
    {
        public PopUpPrivacy(string titulo, string textoPrivacidade)
        {
            InitializeComponent();
            BindingContext = new PopUpPrivacyViewModel(this, titulo, textoPrivacidade);
        }
        
        /// <summary>
        /// Exibe um popup com a polï¿½tica de privacidade para o usuï¿½rio aceitar ou recusar.
        /// </summary>
        /// <param name="titulo">Tï¿½tulo do popup</param>
        /// <param name="textoPrivacidade">Texto completo da polï¿½tica de privacidade</param>
        /// <returns>True se o usuï¿½rio aceitar, False se recusar ou fechar o popup</returns>
        public static async Task<bool> ShowAsync(string titulo, string textoPrivacidade)
        {
            var popup = new PopUpPrivacy(titulo, textoPrivacidade);
            
            // Usa o mï¿½todo genï¿½rico do NavigationUtils que jï¿½ lida com o tipo de retorno
            var result = await NavigationUtils.ShowPopupAsync<bool>(popup);
            
            // Retorna o resultado ou false (Recusar) se o usuï¿½rio fechou o popup sem escolher
            return result;
        }
    }
    
    public class PopUpPrivacyViewModel
    {
        private readonly PopUpPrivacy _popup;
        private bool _isClosing;
        
        public string Titulo { get; }
        public string TextoPrivacidade { get; }
        
        public ICommand AceitarCommand { get; }
        public ICommand RecusarCommand { get; }
        
        public PopUpPrivacyViewModel(PopUpPrivacy popup, string titulo, string textoPrivacidade)
        {
            _popup = popup;
            Titulo = titulo;
            TextoPrivacidade = textoPrivacidade;
            
            AceitarCommand = new Command(Aceitar);
            RecusarCommand = new Command(Recusar);
        }
        
        private async void `Aceitar() { if (_isClosing) return; _isClosing = true; try { await _popup.CloseAsync(`true); } catch { } }
        
        private async void `Recusar() { if (_isClosing) return; _isClosing = true; try { await _popup.CloseAsync(`false); } catch { } }
    }
}
