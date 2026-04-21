using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using System.Threading.Tasks;

namespace SilvaData.Pages.PopUps
{
    public partial class PopUpErrorGalpaoViewModel : ObservableObject
    {
        private readonly PopUpErrorGalpao _popup;

        public string Titulo { get; }
        public string Mensagem { get; }
        public int Minimo { get; }
        public int Realizado { get; }
        public int Maximo { get; }
        public double Media { get; }
        public bool MostrarMedia { get; }

        public PopUpErrorGalpaoViewModel(
            PopUpErrorGalpao popup, 
            string titulo, 
            string mensagem, 
            int minimo, 
            int realizado, 
            int maximo, 
            double media,
            bool mostrarMedia)
        {
            _popup = popup;
            Titulo = titulo;
            Mensagem = mensagem;
            Minimo = minimo;
            Realizado = realizado;
            Maximo = maximo;
            Media = media;
            MostrarMedia = mostrarMedia;
        }

        [RelayCommand]
        private Task OK()
        {
            return _popup.CloseAsync(true);
        }
    }
}
