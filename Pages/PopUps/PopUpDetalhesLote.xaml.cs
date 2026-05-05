using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SilvaData.Models;
using SilvaData.Utilities;

namespace SilvaData.Pages.PopUps
{
    public partial class PopUpDetalhesLote : Popup<bool>
    {
        public PopUpDetalhesLote(Lote lote, string nomeTela)
        {
            InitializeComponent();
            BindingContext = new PopUpDetalhesLoteViewModel(this, lote, nomeTela);
        }

        public static async Task ShowAsync(Lote lote, string nomeTela)
        {
            var popup = new PopUpDetalhesLote(lote, nomeTela);
            await NavigationUtils.ShowPopupAsync(popup);
        }
    }

    public partial class PopUpDetalhesLoteViewModel : ObservableObject
    {
        private readonly PopUpDetalhesLote _popup;

        public Lote Lote { get; }
        public string NomeTela { get; }

        public PopUpDetalhesLoteViewModel(PopUpDetalhesLote popup, Lote lote, string nomeTela)
        {
            _popup = popup;
            Lote = lote;
            NomeTela = nomeTela;
        }

        [RelayCommand]
        private async Task Fechar() { try { await _popup.CloseAsync(true); } catch { } }
    }
}
