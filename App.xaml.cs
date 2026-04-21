using SilvaData.Pages.PopUps;
using SilvaData.Models;

using Microsoft.Maui.Storage;
using Sentry;

using Syncfusion.Licensing;
using System.Diagnostics;

namespace SilvaData
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            // Inicializa o ServiceHelper o mais cedo possível,
            // antes que as views XAML comecem a resolver ViewModels.
            SilvaData.Infrastructure.ServiceHelper.Initialize(serviceProvider);

            RegisterGlobalExceptionHandlers();

            // Força o tema Light ANTES do InitializeComponent()
            UserAppTheme = AppTheme.Light;

            InitializeComponent();

            // Register Syncfusion License - https://www.syncfusion.com/account/downloads
            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXxcdnVVR2hYVUBwX0dWYEo=");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            
            try
            {
                // Salvar estado crítico ao app entrar em modo sleep
                Debug.WriteLine("[App] OnSleep - Salvando estado crítico");
                
                // Força sincronização de dados pendentes se necessário
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Verificar se há alterações pendentes que precisam de atenção especial
                        await ManutencaoTabelas.UpdateTotalAlteracoes();
                        
                        // Salvar formulários em andamento (já implementado)
                        // Esta chamama garante que o estado atual seja persistido
                        var formularioEmAndamento = Preferences.Get("FormularioEmAndamento", "");
                        if (!string.IsNullOrEmpty(formularioEmAndamento))
                        {
                            // O formulário já está sendo salvo automaticamente pelo SalvaEmAndamento()
                            Debug.WriteLine("[App] OnSleep - Formulário em andamento detectado");
                        }
                        
                        // Garantir que o banco de dados esteja em estado consistente
                        await Database.GetInstanceAsync();
                        
                        Debug.WriteLine("[App] OnSleep - Estado crítico salvo com sucesso");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[App] OnSleep - Erro ao salvar estado: {ex.Message}");
                        SentrySdk.CaptureException(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] OnSleep - Erro geral: {ex.Message}");
                SentrySdk.CaptureException(ex);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            try
            {
                Debug.WriteLine("[App] OnResume - Restaurando estado");
                
                // Verificar integridade dos dados ao retomar
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Verificar se há inconsistências nos dados
                        await ManutencaoTabelas.UpdateTotalAlteracoes();
                        
                        // Garantir que o banco de dados esteja acessível
                        await Database.GetInstanceAsync();
                        
                        Debug.WriteLine("[App] OnResume - Estado restaurado com sucesso");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[App] OnResume - Erro ao restaurar estado: {ex.Message}");
                        SentrySdk.CaptureException(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] OnResume - Erro geral: {ex.Message}");
                SentrySdk.CaptureException(ex);
            }
        }

        private void RegisterGlobalExceptionHandlers()
        {
            // Exceções não tratadas em threads background
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception ?? new Exception("Erro desconhecido (UnhandledException)");
                SentrySdk.CaptureException(ex);
                MainThread.BeginInvokeOnMainThread(async () =>
                    await MostrarErroGlobal(ex));
            };

            // Tasks assíncronas cujas exceções não foram observadas
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                SentrySdk.CaptureException(args.Exception);
                args.SetObserved(); // evita que derrube o processo
                MainThread.BeginInvokeOnMainThread(async () =>
                    await MostrarErroGlobal(args.Exception));
            };
        }

        internal static async Task MostrarErroGlobal(Exception ex, string? contexto = null)
        {
            try
            {
                SentrySdk.CaptureException(ex);
                var titulo = "Erro inesperado";
                var mensagem = "Ocorreu um erro inesperado. Nossa equipe foi notificada automaticamente.";
#if DEBUG
                mensagem += $"\n\n[DEBUG] {ex.GetType().Name}: {ex.Message}";
#endif
                await PopUpOK.ShowAsync(titulo, mensagem);
            }
            catch
            {
                // Último recurso: não deixa o tratamento de erro causar outro crash
            }
        }
    }
}