using CommunityToolkit.Maui.Views;
using SilvaData.Utils;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace SilvaData.Pages.PopUps
{
    public partial class PopUpErrorGalpao : Popup<bool>
    {
        public PopUpErrorGalpao(string titulo, string mensagem, int minimo, int realizado, int maximo, double media, bool mostrarMedia)
        {
            InitializeComponent();
            BindingContext = new PopUpErrorGalpaoViewModel(this, titulo, mensagem, minimo, realizado, maximo, media, mostrarMedia);
        }

        public static async Task ShowAsync(string titulo, string mensagem, int minimo, int realizado, int maximo, double media, bool mostrarMedia)
        {
            var popup = new PopUpErrorGalpao(titulo, mensagem, minimo, realizado, maximo, media, mostrarMedia);
            await NavigationUtils.ShowPopupAsync(popup);
        }
    }
}
