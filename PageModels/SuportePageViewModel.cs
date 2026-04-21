using CommunityToolkit.Mvvm.ComponentModel; // Adicionado
using CommunityToolkit.Mvvm.Input;
using SilvaData.Models; // Para PageModelBase
using SilvaData.Utilities;
using System.IO; // Adicionado para File
using System.Threading.Tasks; // Adicionado para Task
using System; // Adicionado para Uri

namespace SilvaData.PageModels
{
    /// <summary>
    /// ViewModel da página de suporte.
    /// Contém comandos para abrir o site, enviar e-mail, abrir WhatsApp e enviar backup do banco de dados.
    /// </summary>
    public partial class SuportePageViewModel : PageModelBase
    {
        /// <summary>
        /// Obtém a string da versão formatada do aplicativo (ex: "Versão 1.0.0").
        /// </summary>
        [ObservableProperty]
        private string versaoApp = string.Empty;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="SuportePageViewModel"/>.
        /// </summary>
        public SuportePageViewModel()
        {
            // Define a versão do app ao inicializar o ViewModel
            var version = AppInfo.Current.VersionString;
            VersaoApp = $"{Traducao.Versão} {version}";
        }

        /// <summary>
        /// Comando que abre o site oficial do ISI no navegador padrão do dispositivo.
        /// </summary>
        [RelayCommand]
        private async Task Site()
        {
            await ISIUtils.TryOpenUriAsync("http://isiinstitute.com/");
        }

        /// <summary>
        /// Comando que inicia o cliente de e-mail com o endereço do suporte.
        /// </summary>
        [RelayCommand]
        async Task Email()
        {
            var address = "isi@isiinstitute.com";
            await Launcher.OpenAsync(new Uri($"mailto:{address}"));
        }

        /// <summary>
        /// Comando que tenta abrir uma conversa no WhatsApp com o número de suporte.
        /// </summary>
        [RelayCommand]
        async Task WhatsApp()
        {
            await Launcher.OpenAsync(new Uri("whatsapp://send?phone=554391626247"));
        }

        /// <summary>
        /// Comando que cria um backup completo (zip) e inicia o diálogo de compartilhamento.
        /// </summary>
        [RelayCommand]
        private async Task EnviarBancoDadosSuporte()
        {
            await RunWithBusyAsync(async () =>
            {
                try
                {
                    var zip = await ISIUtils.CreateFullBackupAsync();

                    if (!string.IsNullOrEmpty(zip) && File.Exists(zip))
                    {
                        await Share.RequestAsync(new ShareFileRequest
                        {
                            Title = Traducao.CompartilharBancoDeDados,
                            File = new ShareFile(zip)
                        });
                    }
                    else
                    {
                        await ISIUtils.ShowErrorAsync(Traducao.Erro, Traducao.ArquivoBackupNaoEncontrado);
                    }
                }
                catch (Exception ex)
                {
                    await ISIUtils.ShowErrorAsync(Traducao.Erro, string.Format(Traducao.ErroAoEnviarBancoDados + ": {0}", ex.Message));
                }
            });
        }
    }
}