using Foundation;
using UIKit;

namespace SilvaData_MAUI
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp()
        {
            System.Console.WriteLine("[AppDelegate] CreateMauiApp INICIANDO...");
            System.Diagnostics.Debug.WriteLine("[AppDelegate] CreateMauiApp INICIANDO...");

            try
            {
                var app = MauiProgram.CreateMauiApp();
                System.Console.WriteLine("[AppDelegate] CreateMauiApp CONCLUÍDO com sucesso.");
                System.Diagnostics.Debug.WriteLine("[AppDelegate] CreateMauiApp CONCLUÍDO com sucesso.");
                return app;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AppDelegate] EXCEÇÃO em CreateMauiApp: {ex}");
                System.Diagnostics.Debug.WriteLine($"[AppDelegate] EXCEÇÃO em CreateMauiApp: {ex}");
                throw;
            }
        }

        public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
        {
            System.Console.WriteLine("[AppDelegate] FinishedLaunching INICIANDO...");
            System.Diagnostics.Debug.WriteLine("[AppDelegate] FinishedLaunching INICIANDO...");

            try
            {
                var result = base.FinishedLaunching(application, launchOptions);
                System.Console.WriteLine("[AppDelegate] FinishedLaunching CONCLUÍDO com sucesso.");
                System.Diagnostics.Debug.WriteLine("[AppDelegate] FinishedLaunching CONCLUÍDO com sucesso.");
                return result;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AppDelegate] EXCEÇÃO em FinishedLaunching: {ex}");
                System.Diagnostics.Debug.WriteLine($"[AppDelegate] EXCEÇÃO em FinishedLaunching: {ex}");
                throw;
            }
        }
    }
}
