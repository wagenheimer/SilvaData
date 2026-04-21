using CommunityToolkit.Maui.Views;
using SilvaData.ViewModels;

namespace SilvaData.Pages.PopUps
{
    public partial class PermissoesPopup : Popup
    {
        public PermissoesPopup()
        {
            InitializeComponent();
            BindingContext = new PermissoesPopupViewModel(this);
        }
    }
}
