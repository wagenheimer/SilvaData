using CommunityToolkit.Maui.Views;
using SilvaData.Utils;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace SilvaData.Pages.PopUps
{
    public partial class PopUpSuccessGalpao : Popup<bool>
    {
        public PopUpSuccessGalpao(string titulo, string mensagem, int minimo, int realizado, int maximo, double media, bool mostrarMedia, Color realizadoColor, string msgDescarte = "")
        {
            InitializeComponent();
            BindingContext = new PopUpSuccessGalpaoViewModel(this, titulo, mensagem, minimo, realizado, maximo, media, mostrarMedia, realizadoColor, msgDescarte);
        }

        public static async Task ShowAsync(string titulo, string mensagem, int minimo, int realizado, int maximo, double media, bool mostrarMedia, Color realizadoColor, string msgDescarte = "")
        {
            var popup = new PopUpSuccessGalpao(titulo, mensagem, minimo, realizado, maximo, media, mostrarMedia, realizadoColor, msgDescarte);
            await NavigationUtils.ShowPopupAsync(popup);
        }
    }
}
