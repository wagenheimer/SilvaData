using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;

using System.Diagnostics;

namespace SilvaData.Controls
{
    /// <summary>
    /// Botão individual para seleção de nota em ISIMacroNota.
    /// ✅ Migrado do Xamarin com propriedades calculadas reativas.
    /// </summary>
    public partial class ISIMacroNotaButton : ContentView, ICampoObrigatorio
    {
        // ============================================================================
        // BINDABLE PROPERTIES
        // ============================================================================

        /// <summary>Alternativa selecionável</summary>
        public static readonly BindableProperty AlternativaProperty =
            BindableProperty.Create(
                nameof(Alternativa),
                typeof(ParametroAlternativas),
                typeof(ISIMacroNotaButton),
                null,
                BindingMode.OneWay,
                propertyChanged: (b, o, n) =>
                {
                    var control = (ISIMacroNotaButton)b;
                    control.OnPropertyChanged(nameof(SelecionadoColor));
                    control.OnPropertyChanged(nameof(SelecionadoColorFonte));
                    control.OnPropertyChanged(nameof(EstaSelecionado));
                });

        public ParametroAlternativas? Alternativa
        {
            get => (ParametroAlternativas?)GetValue(AlternativaProperty);
            set => SetValue(AlternativaProperty, value);
        }

        /// <summary>Referência ao controle pai para sincronização</summary>
        public static readonly BindableProperty NotaProperty =
            BindableProperty.Create(
                nameof(Nota),
                typeof(ISIMacroNota),
                typeof(ISIMacroNotaButton),
                null,
                BindingMode.OneWay,
                propertyChanged: (b, o, n) =>
                {
                    var control = (ISIMacroNotaButton)b;
                    control.OnPropertyChanged(nameof(SelecionadoColor));
                    control.OnPropertyChanged(nameof(SelecionadoColorFonte));
                    control.OnPropertyChanged(nameof(EstaSelecionado));
                });

        public ISIMacroNota? Nota
        {
            get => (ISIMacroNota?)GetValue(NotaProperty);
            set => SetValue(NotaProperty, value);
        }

        /// <summary>Modo somente leitura</summary>
        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(ISIMacroNotaButton),
                false,
                BindingMode.TwoWay,
                propertyChanged: (b, o, n) =>
                {
                    var control = (ISIMacroNotaButton)b;
                    control.OnPropertyChanged(nameof(IsReadOnly));
                    control.OnPropertyChanged(nameof(NotIsReadOnly));
                });

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool NotIsReadOnly => !IsReadOnly;

        // ============================================================================
        // PROPRIEDADES CALCULADAS (Reactivas)
        // ============================================================================

        /// <summary>Verifica se está selecionado (baseado em AlternativaSelecionada)</summary>
        public bool EstaSelecionado =>
            Nota?.ISIMacroParametro != null &&
            Alternativa != null &&
            Nota.ISIMacroParametro.AlternativaSelecionada == Alternativa;

        /// <summary>✅ Cor de fundo - Muda se está selecionado</summary>
        public Color SelecionadoColor =>
            EstaSelecionado
                ? Color.FromArgb("#48BA01") // Verde quando selecionado
                : Color.FromArgb("#F8FCFE"); // Branco quando não

        /// <summary>✅ Cor de texto - Muda se está selecionado</summary>
        public Color SelecionadoColorFonte =>
            EstaSelecionado
                ? Colors.White
                : (Application.Current?.Resources != null && Application.Current.Resources.TryGetValue("PrimaryColor", out var color) && color is Color c ? c : Colors.Blue);

        // ============================================================================
        // LIFECYCLE
        // ============================================================================

        public ISIMacroNotaButton()
        {
            try
            {
                InitializeComponent();

                // ✅ Inscreve-se em UpdateScoreMessage para atualizar cores
                WeakReferenceMessenger.Default.Register<UpdateScoreMessage>(this, (recipient, message) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanged(nameof(EstaSelecionado));
                        OnPropertyChanged(nameof(SelecionadoColor));
                        OnPropertyChanged(nameof(SelecionadoColorFonte));
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] ISIMacroNotaButton: {ex.Message}");
                Exception? inner = ex.InnerException;
                while (inner != null)
                {
                    Debug.WriteLine($"[CRASH_INNER] {inner.Message}");
                    Debug.WriteLine($"[CRASH_TRACE] {inner.StackTrace}");
                    inner = inner.InnerException;
                }
                throw;
            }
        }

        // ============================================================================
        // COMANDO
        // ============================================================================

        /// <summary>✅ Seleciona/deseleciona esta alternativa</summary>
        [RelayCommand]
        private void SelecionaNota()
        {
            try
            {
                // Validações
                if (IsReadOnly || Nota?.ISIMacroParametro == null || Alternativa == null)
                    return;

                int novoIndex = Nota.ISIMacroParametro.ListaAlternativas.IndexOf(Alternativa);

                if (novoIndex == -1)
                {
                    Debug.WriteLine($"[ISIMacroNotaButton] Alternativa não encontrada");
                    return;
                }

                // Toggle: se já está selecionado, desseleciona
                if (Nota.ISIMacroParametro.SelectedIndex == novoIndex)
                {
                    Nota.ISIMacroParametro.SelectedIndex = -1;
                }
                else
                {
                    Nota.ISIMacroParametro.SelectedIndex = novoIndex;

                    // Feedback tátil + avança pro próximo
                    HapticHelper.VibrateClick();
                    WeakReferenceMessenger.Default.Send(new VaiProProximoMessage(Nota));
                }

                // Atualiza o pai
                Nota.AtualizaSelecao();

                // Notifica mudança de cores
                OnPropertyChanged(nameof(SelecionadoColor));
                OnPropertyChanged(nameof(SelecionadoColorFonte));
                OnPropertyChanged(nameof(EstaSelecionado));

                // ✅ Envia mensagem de atualização de score
                WeakReferenceMessenger.Default.Send(new UpdateScoreMessage());

                Debug.WriteLine($"[ISIMacroNotaButton] Selecionada: {Alternativa.descricao}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ISIMacroNotaButton] Erro: {ex.Message}");
            }
        }

        // ============================================================================
        // VALIDAÇÃO (ICampoObrigatorio)
        // ============================================================================

        /// <summary>Delegado ao ISIMacroNota pai</summary>
        public bool PreenchidoCorretamente()
        {
            return Nota?.PreenchidoCorretamente() ?? true;
        }

        // ============================================================================
        // CLEANUP
        // ============================================================================

        /// <summary>Desregistra mensagens quando o controle é removido da árvore visual.</summary>
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (Handler == null)
                WeakReferenceMessenger.Default.Unregister<UpdateScoreMessage>(this);
        }
    }
}
