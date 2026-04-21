using SilvaData.Infrastructure;
using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    public partial class ConfigView : ContentView
    {
        ConfigViewModel ViewModel => ServiceHelper.GetRequiredService<ConfigViewModel>();

        public ConfigView()
        {
            InitializeComponent();

            BindingContext = ViewModel;
        }
    }
}
