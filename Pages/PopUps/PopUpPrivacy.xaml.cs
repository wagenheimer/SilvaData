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
        /// Exibe um popup com a política de privacidade para o usuário aceitar ou recusar.
        /// </summary>
        /// <param name="titulo">Título do popup</param>
        /// <param name="textoPrivacidade">Texto completo da política de privacidade</param>
        /// <returns>True se o usuário aceitar, False se recusar ou fechar o popup</returns>
        public static async Task<bool> ShowAsync(string titulo, string textoPrivacidade)
        {
            var popup = new PopUpPrivacy(titulo, textoPrivacidade);
            
            // Usa o método genérico do NavigationUtils que já lida com o tipo de retorno
            var result = await NavigationUtils.ShowPopupAsync<bool>(popup);
            
            // Retorna o resultado ou false (Recusar) se o usuário fechou o popup sem escolher
            return result;
        }
    }
    
    public class PopUpPrivacyViewModel
    {
        private readonly PopUpPrivacy _popup;
        
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
        
        private void Aceitar()
        {
            _popup.CloseAsync(true);
        }
        
        private void Recusar()
        {
            _popup.CloseAsync(false);
        }
    }
}
