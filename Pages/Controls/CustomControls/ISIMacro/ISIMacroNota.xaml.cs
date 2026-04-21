using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;

using System.Diagnostics;

namespace SilvaData.Controls
{
    /// <summary>
    /// Controle para seleção de notas macro com visualização de fotos.
    /// Usa MVVM Messenger para desacoplamento de componentes.
    /// </summary>
    public partial class ISIMacroNota : ContentView, ICampoObrigatorio
    {
        private ParametroComAlternativas? _parametro;

        public ParametroComAlternativas? ISIMacroParametro
        {
            get => _parametro;
            set { if (_parametro != value) { _parametro = value; AtualizaSelecao(); } }
        }

        public ParametroAlternativas? AlternativaSelecionada => ISIMacroParametro?.AlternativaSelecionada;
        public bool EstaSelecionado => AlternativaSelecionada != null;
        public int TotalNota => ISIMacroParametro?.Nota ?? 0;

        #region Bindable Properties

        // IsReadOnly: Define se o controle está em modo leitura.
        public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(ISIMacroNota), false, BindingMode.TwoWay, propertyChanged: ReadOnlyChanged);
        public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
        public bool NotIsReadOnly => !IsReadOnly;

        private static void ReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ISIMacroNota control)
            {
                control.OnPropertyChanged(nameof(IsReadOnly));
                control.OnPropertyChanged(nameof(NotIsReadOnly));
                Debug.WriteLine($"[ISIMacroNota] IsReadOnly={newValue}");
            }
        }

        // VisualizarFotosCommand: Comando injetado para abrir galeria de fotos.
        public static readonly BindableProperty VisualizarFotosCommandProperty = BindableProperty.Create(
            nameof(VisualizarFotosCommand), typeof(IAsyncRelayCommand<ParametroComAlternativas>), typeof(ISIMacroNota), null, BindingMode.OneWay);

        public IAsyncRelayCommand<ParametroComAlternativas>? VisualizarFotosCommand
        {
            get => (IAsyncRelayCommand<ParametroComAlternativas>?)GetValue(VisualizarFotosCommandProperty);
            set => SetValue(VisualizarFotosCommandProperty, value);
        }

        #endregion

        #region Computed Properties

        // Cor dinâmica: Verde (selecionado) ou vermelho (não selecionado).
        public Color BorderColor => EstaSelecionado 
            ? (Application.Current?.Resources != null && Application.Current.Resources.TryGetValue("PrimaryColor", out var color) && color is Color c ? c : Colors.Blue) 
            : Colors.Red;

        // Fundo dinâmico: Azul claro (selecionado) ou rosa claro (não selecionado).
        public new Color BackgroundColor => EstaSelecionado ? Color.FromArgb("#f0faff") : Color.FromArgb("#fdf7f7");

        #endregion

        public ISIMacroNota()
        {
            try
            {
                InitializeComponent();
                // Listener para atualizar UI quando score muda em outro componente.
                WeakReferenceMessenger.Default.Register<UpdateScoreMessage>(this, (r, m) => NotificaScore());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] ISIMacroNota: {ex.Message}");
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

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            if (BindingContext is ParametroComAlternativas param)
            {
                ISIMacroParametro = param;
                Debug.WriteLine($"[ISIMacroNota] BindingContext: {param.nome}");
            }
            else ISIMacroParametro = null;
        }

        // Notifica todas as propriedades dependentes quando score muda.
        private void NotificaScore()
        {
            try
            {
                OnPropertyChanged(nameof(AlternativaSelecionada));
                OnPropertyChanged(nameof(TotalNota));
                OnPropertyChanged(nameof(BorderColor));
                OnPropertyChanged(nameof(BackgroundColor));
                Debug.WriteLine("[ISIMacroNota] Score atualizado");
            }
            catch (Exception ex) { Debug.WriteLine($"[ISIMacroNota] Erro: {ex.Message}"); }
        }

        // Atualiza cores e validação após seleção de alternativa.
        public void AtualizaSelecao()
        {
            OnPropertyChanged(nameof(TotalNota));
            OnPropertyChanged(nameof(AlternativaSelecionada));
            OnPropertyChanged(nameof(EstaSelecionado));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(BackgroundColor));
            Debug.WriteLine($"[ISIMacroNota] Seleção: {AlternativaSelecionada?.descricao ?? "Nenhuma"}");
        }

        // Abre visualizador de fotos via messenger.
        [RelayCommand]
        public async Task SelecionaFoto()
        {
            var parametro = ISIMacroParametro;
            if (IsReadOnly || parametro == null) return;

            try
            {
                await Task.Delay(100);
                Debug.WriteLine($"[ISIMacroNota] Abrindo: {parametro.nome}");
                WeakReferenceMessenger.Default.Send(new ISIMacroFotoRequestedMessage(parametro.nome, parametro));
            }
            catch (Exception ex) { Debug.WriteLine($"[ISIMacroNota] Erro: {ex.Message}"); }
        }

        // Valida se campo obrigatório foi preenchido.
        public bool PreenchidoCorretamente()
        {
            var temErro = ISIMacroParametro?.required == 1 && !EstaSelecionado;
            Debug.WriteLine($"[ISIMacroNota] Validação: {ISIMacroParametro?.nome ?? "Desconhecido"} - Erro={temErro}");
            return !temErro;
        }
    }
}
