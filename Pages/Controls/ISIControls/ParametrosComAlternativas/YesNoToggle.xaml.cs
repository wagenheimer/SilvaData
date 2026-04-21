using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Models;
using System.Diagnostics;

namespace SilvaData.Controls
{
    /// <summary>
    /// Controle Sim/Não com cores de destaque para botão selecionado.
    /// </summary>
    public partial class YesNoToggle : ValidatableFieldBase
    {
        public ParametroComAlternativas? ParametroComAlternativas;

        #region IsReadOnly BindableProperty

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(YesNoToggle),
                false,
                BindingMode.TwoWay,
                null,
                propertyChanged: ReadOnlyChanged);

        private static void ReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((YesNoToggle)bindable).OnPropertyChanged(nameof(IsReadOnly));
            ((YesNoToggle)bindable).ScheduleValidationRefresh();
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        #endregion

        public YesNoToggle()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] YesNoToggle: {ex.Message}");
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

        /// <summary>
        /// Handler do botão "Não" - Toggle false/null.
        /// </summary>
        private void BtNo_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (IsReadOnly) return;

                WeakReferenceMessenger.Default.Send(new VaiProProximoMessage(this));

                if (ParametroComAlternativas != null)
                {
                    // Toggle: false -> null -> false -> null...
                    ParametroComAlternativas.ValorSimNao =
                        ParametroComAlternativas.ValorSimNao == false ? null : (bool?)false;
                }

                HapticHelper.VibrateClick();
                Debug.WriteLine($"[YesNoToggle] ✗ Não clicado: {ParametroComAlternativas?.nome} = {ParametroComAlternativas?.ValorSimNao}");

                UpdateColorText();
                ScheduleValidationRefresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YesNoToggle] Erro em BtNo_Clicked: {ex.Message}");
            }
        }

        /// <summary>
        /// Handler do botão "Sim" - Toggle true/null.
        /// </summary>
        private void BtYes_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (IsReadOnly) return;

                WeakReferenceMessenger.Default.Send(new VaiProProximoMessage(this));

                if (ParametroComAlternativas != null)
                {
                    // Toggle: true -> null -> true -> null...
                    ParametroComAlternativas.ValorSimNao =
                        ParametroComAlternativas.ValorSimNao == true ? null : (bool?)true;
                }

                HapticHelper.VibrateClick();
                Debug.WriteLine($"[YesNoToggle] ✓ Sim clicado: {ParametroComAlternativas?.nome} = {ParametroComAlternativas?.ValorSimNao}");

                UpdateColorText();
                ScheduleValidationRefresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YesNoToggle] Erro em BtYes_Clicked: {ex.Message}");
            }
        }

        /// <summary>
        /// Chamado quando o BindingContext muda (ItemTemplate).
        /// </summary>
        protected override void OnContextAttached()
        {
            if (BindingContext is ParametroComAlternativas p)
            {
                ParametroComAlternativas = p;
                UpdateColorText();
                return;
            }

            OnContextCleared();
        }

        protected override void OnContextCleared()
        {
            ParametroComAlternativas = null;
            UpdateColorText();
        }

        /// <summary>
        /// Atualiza cores e texto quando o valor muda.
        /// Dispatcher.Dispatch garante que as cores sejam aplicadas APÓS as transições internas
        /// do SfButton (que resetam Background para branco via VSM global).
        /// </summary>
        private void UpdateColorText()
        {
            try
            {
                if (Dispatcher != null)
                {
                    Dispatcher.Dispatch(() =>
                    {
                        OnPropertyChanged(nameof(YesNoText));
                        OnPropertyChanged(nameof(TextColor));
                        OnPropertyChanged(nameof(YesColor));
                        OnPropertyChanged(nameof(YesTextColor));
                        OnPropertyChanged(nameof(NoColor));
                        OnPropertyChanged(nameof(NoTextColor));
                    });
                }
                else if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        OnPropertyChanged(nameof(YesNoText));
                        OnPropertyChanged(nameof(TextColor));
                        OnPropertyChanged(nameof(YesColor));
                        OnPropertyChanged(nameof(YesTextColor));
                        OnPropertyChanged(nameof(NoColor));
                        OnPropertyChanged(nameof(NoTextColor));
                    });
                }
                else
                {
                    OnPropertyChanged(nameof(YesNoText));
                    OnPropertyChanged(nameof(TextColor));
                    OnPropertyChanged(nameof(YesColor));
                    OnPropertyChanged(nameof(YesTextColor));
                    OnPropertyChanged(nameof(NoColor));
                    OnPropertyChanged(nameof(NoTextColor));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YesNoToggle] Erro em UpdateColorText: {ex.Message}");
            }
        }

        /// <summary>
        /// Implementação de ICampoObrigatorio.
        /// </summary>
        protected override bool ComputeHasError()
        {
            bool campoEstaVazio = (ParametroComAlternativas?.ValorSimNao == null);
            return ParametroComAlternativas?.required == 1 && campoEstaVazio && IsAnyValidationActive && !IsReadOnly;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            OnPropertyChanged(nameof(TextColor));
        }

        #region Propriedades Calculadas (Cores e Texto)

        /// <summary>
        /// Cor do texto do Label (vermelho se erro, cor secundária se OK).
        /// </summary>
        public Color TextColor => HasError
            ? Colors.Red
            : ValidationVisualHelper.GetPrimaryColor();

        /// <summary>
        /// Cor de fundo do botão "Sim" (VERDE BRILHANTE quando selecionado).
        /// </summary>
        public Color YesColor
        {
            get
            {
                if (ParametroComAlternativas?.ValorSimNao == true)
                    return Color.FromArgb("#C8E6C9");

                return Colors.Transparent;
            }
        }

        /// <summary>
        /// Cor do texto do botão "Sim" (Sempre verde).
        /// </summary>
        public Color YesTextColor
        {
            get
            {
                if (ParametroComAlternativas?.ValorSimNao == true)
                    return Color.FromArgb("#1B5E20");

                return Color.FromArgb("#4CAF50");
            }
        }

        /// <summary>
        /// Cor de fundo do botão "Não" (Fundo vermelho claro quando selecionado).
        /// </summary>
        public Color NoColor
        {
            get
            {
                if (ParametroComAlternativas?.ValorSimNao == false)
                    return Color.FromArgb("#FFCDD2");

                return Colors.Transparent;
            }
        }

        /// <summary>
        /// Cor do texto do botão "Não" (Sempre vermelho).
        /// </summary>
        public Color NoTextColor
        {
            get
            {
                if (ParametroComAlternativas?.ValorSimNao == false)
                    return Color.FromArgb("#B71C1C");

                return Color.FromArgb("#F44336");
            }
        }

        /// <summary>
        /// Texto exibido (Sim/Não ou vazio).
        /// </summary>
        public string? YesNoText => (ParametroComAlternativas == null || ParametroComAlternativas.ValorSimNao == null)
            ? null
            : (ParametroComAlternativas.ValorSimNao == true ? Traducao.Sim : Traducao.Nao);

        #endregion
    }
}
