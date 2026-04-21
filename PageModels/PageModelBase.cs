using CommunityToolkit.Mvvm.ComponentModel;

namespace SilvaData.PageModels
{
    public partial class PageModelBase : ObservableObject
    {
        [ObservableProperty]
        bool isBusy = false;


        protected async Task RunWithBusyAsync(Func<Task> action)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                await action();
            }
            catch (Exception ex)
            {
                await App.MostrarErroGlobal(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
