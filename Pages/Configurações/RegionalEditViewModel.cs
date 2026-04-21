using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using HapticHelper = global::SilvaData.Utilities.HapticHelper;


namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a página de edição ou criação de uma Regional.
    /// Herda de <see cref="BaseEditViewModel"/> para obter o estado
    /// (IsBusy, ReadOnly) e comandos (BackNowCommand, etc.).
    /// </summary>
    public partial class RegionalEditViewModel : BaseEditViewModel
    {
        [ObservableProperty]
        private Regional regional = new();

        internal static Action<int?>? EventoAoAdicionar = null;

        /// <summary>
        /// Define o estado inicial do ViewModel com base na Regional fornecida.
        /// </summary>
        /// <param name="regional">A Regional a ser editada, ou null para criar uma nova.</param>
        public void SetInitialState(Regional? regional)
        {
            _hasLoaded = false;
            if (regional == null)
            {
                NovoRegistro = true;
                Regional = new Regional { id = 0 };
                Title = Traducao.NovaRegional;
                SubTitle = Traducao.AdicioneAsInformaçõesAbaixo;
            }
            else
            {
                NovoRegistro = false;
                Regional = regional;
                Title = Traducao.EditarRegional;
                SubTitle = Traducao.AdicioneAsInformaçõesAbaixo;
            }
        }

        /// <summary>
        /// Carrega os dados necessários para a página.
        /// (Nenhum dado assíncrono é necessário para esta entidade simples).
        /// </summary>
        public override Task GetItemOrCreateANew()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sobrescreve o comando base para salvar a Regional no banco de dados.
        /// </summary>
        public override async Task SaveAndReturn()
        {
            if (IsBusy) return;

            if (!await ValidateViewAsync())
            {
                return;
            }

            try
            {
                IsBusy = true;
                DataSaved = true;
                Regional.excluido = 0;

                await Regional.SaveItemAsync(Regional);

                string popupMessage;
                if (NovoRegistro)
                {
                    popupMessage = Traducao.RegionalAdicionadaComSucesso;
                    WeakReferenceMessenger.Default.Send(new RegionalAdicionadaMessage(Regional));
                    EventoAoAdicionar?.Invoke(Regional.id);
                    EventoAoAdicionar = null;
                }
                else
                {
                    popupMessage = Traducao.ReginonalAlteradaComSucesso;
                    WeakReferenceMessenger.Default.Send(new RegionalSalvaMessage(Regional));
                }

                HapticHelper.VibrateClick();
                await PopUpOK.ShowAsync(Title, popupMessage);
                await BackNow(); // Chama o comando base para voltar
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao salvar: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}