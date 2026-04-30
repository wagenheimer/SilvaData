using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;


using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.Utils;

using System.Diagnostics;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a página de seleção de imagens/notas do ISIMacro.
    /// ✅ MAUI completo com mensagens WeakReferenceMessenger
    /// </summary>
    public partial class ISIMacroNotaSelecionaImagemViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string pageTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Alternativa))]
        private ParametroComAlternativas? iSIMacroParametro;

        /// <summary>
        /// Alternativa atualmente selecionada (ou a primeira da lista caso nenhuma esteja selecionada, apenas para visualização)
        /// </summary>
        public ParametroAlternativas? Alternativa
        {
            get
            {
                if (ISIMacroParametro == null) return null;
                if (ISIMacroParametro.SelectedIndex != -1 && ISIMacroParametro.SelectedIndex < ISIMacroParametro.ListaAlternativas.Count)
                    return ISIMacroParametro.ListaAlternativas[ISIMacroParametro.SelectedIndex];

                return ISIMacroParametro.ListaAlternativas.Count > 0 ? ISIMacroParametro.ListaAlternativas[0] : null;
            }
        }


        public ImageSource? AlternativaImageSource
        {
            get
            {
                var alternativa = Alternativa;
                var rawUrl = alternativa?.urlImagem;
                var normalized = ParametroAlternativasFromWebService.NormalizeImageFileName(rawUrl);
                var path = ParametroAlternativasFromWebService.BuildLocalImagePath(rawUrl);
                var exists = !string.IsNullOrEmpty(path) && File.Exists(path);

                Debug.WriteLine($"[ISIMacroFotoVM] ResolveImage altId={alternativa?.id} raw='{rawUrl ?? ""}' normalized='{normalized}' path='{path}' exists={exists} platform={DeviceInfo.Platform}");

                if (!exists)
                {
                    Debug.WriteLine($"[ISIMacroFotoVM] ❌ Imagem não encontrada: {path ?? "NULL"}");
                    return null;
                }

                var fileInfo = new FileInfo(path);
                Debug.WriteLine($"[ISIMacroFotoVM] 🖼️ Carregando: {path} | bytes={fileInfo.Length}");

                // MemoryStream: stream re-legível, o SfImageEditor pode ler o stream múltiplas
                // vezes (render inicial + re-layout). FileStream falha na segunda leitura.
                // Padrão documentado pela Syncfusion para carregamento confiável.
                var bytes = File.ReadAllBytes(path);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
        }



        /// <summary>
        /// Inicializa o ViewModel com os dados necessários.
        /// </summary>
        public void SetInitialState(string nome, ParametroComAlternativas parametro)
        {
            PageTitle = nome;
            ISIMacroParametro = parametro;

            // ❌ REMOVIDO: Não auto-selecionar a primeira alternativa
            // Isso estava causando o problema de todos os ISIMacroNota virem marcados
            // if (ISIMacroParametro?.SelectedIndex == -1 && ISIMacroParametro?.ListaAlternativas?.Count > 0)
            // {
            //     ISIMacroParametro.SelectedIndex = 0;
            // }

            NotifyChanges();

            Debug.WriteLine($"[ISIMacroNotaSelecionaImagemViewModel] Inicializado: {nome}");
        }

        /// <summary>
        /// Notifica a UI sobre mudanças nas propriedades calculadas e estados de comando.
        /// </summary>
        private void NotifyChanges()
        {
            OnPropertyChanged(nameof(Alternativa));
            OnPropertyChanged(nameof(AlternativaImageSource));
            PriorCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
        }

        #region Comandos de Navegação (Anterior/Próximo)

        private bool CanExecutePrior() => ISIMacroParametro?.SelectedIndex > 0;

        [RelayCommand(CanExecute = nameof(CanExecutePrior))]
        private void Prior()
        {
            if (ISIMacroParametro == null) return;

            ISIMacroParametro.SelectedIndex--;
            NotifyChanges();

            Debug.WriteLine($"[ISIMacroNotaSelecionaImagemViewModel] Prior: Index={ISIMacroParametro.SelectedIndex}");
        }

        private bool CanExecuteNext()
        {
            if (ISIMacroParametro == null) return false;
            // SelectedIndex == -1 means Alternativa already displays [0] as fallback,
            // so treat it as 0 when deciding whether there is a "next" item.
            var effectiveIndex = Math.Max(0, ISIMacroParametro.SelectedIndex);
            return effectiveIndex < (ISIMacroParametro.ListaAlternativas.Count - 1);
        }

        [RelayCommand(CanExecute = nameof(CanExecuteNext))]
        private void Next()
        {
            if (ISIMacroParametro == null) return;

            // When SelectedIndex == -1, Alternativa already shows [0] via fallback.
            // Jumping to 0 would leave the display unchanged; jump directly to 1.
            var effectiveIndex = Math.Max(0, ISIMacroParametro.SelectedIndex);
            ISIMacroParametro.SelectedIndex = effectiveIndex + 1;
            NotifyChanges();

            Debug.WriteLine($"[ISIMacroNotaSelecionaImagemViewModel] Next: Index={ISIMacroParametro.SelectedIndex}");
        }

        #endregion

        #region Comandos Principais (Voltar/Salvar)

        [RelayCommand]
        public async Task VoltarAsync()
        {
            try
            {
                await NavigationUtils.PopModalAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ISIMacroNotaSelecionaImagemViewModel] Erro ao voltar: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SelectAndBackAsync()
        {
            if (ISIMacroParametro == null) return;

            try
            {
                // Commit the currently displayed alternativa before sending the score update.
                // When SelectedIndex == -1, Alternativa falls back to ListaAlternativas[0] for display
                // but SelectedIndex stays -1 → AlternativaSelecionada would be null and no score is saved.
                if (ISIMacroParametro.SelectedIndex == -1 && ISIMacroParametro.ListaAlternativas.Count > 0)
                    ISIMacroParametro.SelectedIndex = 0;

                Debug.WriteLine($"[ISIMacroNotaSelecionaImagemViewModel] Selecionado: {Alternativa?.descricao} (Score: {Alternativa?.score}) Index={ISIMacroParametro.SelectedIndex}");

                HapticHelper.VibrateClick();
                WeakReferenceMessenger.Default.Send(new UpdateScoreMessage());

                // Fecha a página
                await VoltarAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ISIMacroNotaSelecionaImagemViewModel] Erro ao selecionar: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// ✅ Cleanup ao sair da página
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();

            ISIMacroParametro = null;
            PageTitle = string.Empty;

            Debug.WriteLine("[ISIMacroNotaSelecionaImagemViewModel] Cleanup executado");
        }

        #endregion
    }
}
