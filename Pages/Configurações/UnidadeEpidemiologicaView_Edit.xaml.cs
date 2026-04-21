using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

using System.Diagnostics;

namespace SilvaData.Controls
{
    /// <summary>
    /// View para editar ou criar uma Unidade Epidemiológica.
    /// MIGRADO: Usa Messaging para comunicação com o ViewModel (sem acoplamento direto).
    /// </summary>
    public partial class UnidadeEpidemiologicaView_Edit : ContentPageEdit
    {
        private new readonly UnidadeEpidemiologicaEditViewModel ViewModel;

        /// <summary>
        /// MIGRADO: Construtor não passa mais IValidatablePage para o ViewModel
        /// </summary>
        public UnidadeEpidemiologicaView_Edit(UnidadeEpidemiologica? ue = null)
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<UnidadeEpidemiologicaEditViewModel>();

            // MUDANÇA: Não passa mais 'this' para o SetInitialState
            ViewModel.SetInitialState(ue);

            BindingContext = ViewModel;

            // Popula a lista de campos obrigatórios para a validação da UI
            RequiredInputFields.Add(this.FindByName<ISITextField>("nome"));
            RequiredInputFields.Add(this.FindByName<PropriedadeComboBox>("propriedadecombobox"));

            // Registra os handlers para o ciclo de vida da View
            this.Loaded += OnPageLoaded;
            this.Unloaded += OnPageUnloaded;
        }

        #region Ciclo de Vida e Handlers de Mensagem

        /// <summary>
        /// Chamado quando a página é carregada.
        /// </summary>
        private void OnPageLoaded(object? sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.Register<PropriedadeAdicionadaMessage>(this, OnPropriedadeAdicionadaMessage);
        }

        /// <summary>
        /// Chamado quando a página é descarregada (destruída).
        /// </summary>
        private void OnPageUnloaded(object? sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.Unregister<PropriedadeAdicionadaMessage>(this);
        }

        #endregion

        #region Handlers de Mensagens Específicas desta View

        /// <summary>
        /// Handler para quando uma nova Propriedade é adicionada
        /// </summary>
        private void OnPropriedadeAdicionadaMessage(object recipient, PropriedadeAdicionadaMessage message)
        {
            _ = OnPropriedadeAdicionadaMessageAsync(recipient, message);
        }

        private async Task OnPropriedadeAdicionadaMessageAsync(object recipient, PropriedadeAdicionadaMessage message)
        {
            try
            {
                var id = message?.Propriedade?.id;

                // Garante que o callback seja executado na thread da UI
                await MainThread.InvokeOnMainThreadAsync(async () => await PropriedadeAdicionada(id));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PropriedadeAdicionadaMessage] Erro: {ex}");
            }
        }

        /// <summary>
        /// Callback executado após a mensagem de Propriedade ser recebida.
        /// Atualiza o ComboBox de Propriedades e seleciona a nova.
        /// </summary>
        private async Task PropriedadeAdicionada(int? propriedadeId)
        {
            var propriedadeCombo = this.FindByName<PropriedadeComboBox>("propriedadecombobox");
            if (propriedadeCombo != null)
            {
                // Atualiza a lista interna do ComboBox
                await propriedadeCombo.AtualizaDados();

                if (ViewModel != null && ViewModel.Propriedades != null)
                {
                    // Define o item selecionado diretamente no ViewModel
                    ViewModel.SelectedPropriedade = ViewModel.Propriedades
                        .FirstOrDefault(p => p.id == propriedadeId);
                }
            }
        }

        #endregion
    }
}
