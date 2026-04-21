using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using System.Threading.Tasks;

namespace SilvaData.Pages.PopUps
{
    public partial class PopUpSuccessGalpaoViewModel : ObservableObject
    {
        private readonly PopUpSuccessGalpao _popup;

        public string Titulo { get; }
        public string Mensagem { get; }
        public int Minimo { get; }
        public int Realizado { get; }
        public int Maximo { get; }
        public double Media { get; }
        public bool MostrarMedia { get; }
        public Color RealizadoColor { get; }
        public string MsgDescarte { get; }
        public bool TemMsgDescarte => !string.IsNullOrEmpty(MsgDescarte);

        public PopUpSuccessGalpaoViewModel(
            PopUpSuccessGalpao popup, 
            string titulo, 
            string mensagem, 
            int minimo, 
            int realizado, 
            int maximo, 
            double media,
            bool mostrarMedia,
            Color realizadoColor,
            string msgDescarte)
        {
            _popup = popup;
            Titulo = titulo;
            Mensagem = mensagem;
            Minimo = minimo;
            Realizado = realizado;
            Maximo = maximo;
            Media = media;
            MostrarMedia = mostrarMedia;
            RealizadoColor = realizadoColor;
            MsgDescarte = msgDescarte;
        }

        [RelayCommand]
        private Task OK()
        {
            return _popup.CloseAsync(true);
        }
    }
}
