using SilvaData.Models;
using System.Diagnostics;

namespace SilvaData.Controls
{
    /// <summary>
    /// Controle para entrada de valores double (decimais) com validação de limites.
    /// ✅ Sincroniza com ParametroComAlternativas.ValorDouble e EstaRespondida
    /// </summary>
    public partial class ValorDouble : ValidatableFieldBase
    {
        public ParametroComAlternativas? ParametroComAlternativas;

        #region Valor BindableProperty

        public static readonly BindableProperty ValorProperty =
            BindableProperty.Create(
                nameof(Valor),
                typeof(double?),
                typeof(ValorDouble),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnValorChanged);

        private static void OnValorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ValorDouble)bindable;
            double? valor = (double?)newValue;

            // ✅ Validação de valores especiais
            if (valor.HasValue && (double.IsNaN(valor.Value) || double.IsInfinity(valor.Value)))
            {
                control.SetValue(ValorProperty, null);
                return;
            }

            // ✅ Aplicar limites min/max e sincronizar
            if (control.ParametroComAlternativas != null)
            {
                if (valor.HasValue)
                {
                    if (control.ParametroComAlternativas.valorMinimo.HasValue && valor < control.ParametroComAlternativas.valorMinimo)
                        valor = control.ParametroComAlternativas.valorMinimo;

                    if (control.ParametroComAlternativas.valorMaximo.HasValue && valor > control.ParametroComAlternativas.valorMaximo)
                        valor = control.ParametroComAlternativas.valorMaximo;

                    // ✅ SINCRONIZA com o modelo
                    control.ParametroComAlternativas.ValorDouble = valor;

                    if (control.Valor != valor)
                        control.Valor = valor;
                }
                else
                {
                    // ✅ Limpa se vazio
                    control.ParametroComAlternativas.ValorDouble = null;
                }
            }

            Debug.WriteLine($"[ValorDouble] Valor: {valor} - Respondida: {control.ParametroComAlternativas?.EstaRespondida}");

            control.ScheduleValidationRefresh();
        }

        public double? Valor
        {
            get => (double?)GetValue(ValorProperty);
            set => SetValue(ValorProperty, value);
        }

        #endregion

        #region IsReadOnly BindableProperty

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(ValorDouble),
                false,
                BindingMode.TwoWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var control = (ValorDouble)bindable;
                    control.OnPropertyChanged(nameof(IsReadOnly));
                    control.OnPropertyChanged(nameof(NotIsReadOnly));
                    Debug.WriteLine($"[ValorDouble] IsReadOnly={newValue}");
                });

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool NotIsReadOnly => !IsReadOnly;

        #endregion

        public ValorDouble()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] ValorDouble: {ex.Message}");
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

        protected override void OnContextAttached()
        {
            if (BindingContext is not ParametroComAlternativas param)
            {
                ParametroComAlternativas = null;
                Valor = null;
                return;
            }

            ParametroComAlternativas = param;

            // ORDEM CORRETA: Primeiro verifica se já tem valor salvo
            if (ParametroComAlternativas.ValorDouble.HasValue)
            {
                Valor = ParametroComAlternativas.ValorDouble;
            }
            // Segundo: tenta usar valor padrão se não houver valor salvo
            else if (double.TryParse(ParametroComAlternativas.valorPadrao, out double valorPadraoDouble))
            {
                ParametroComAlternativas.ValorDouble = valorPadraoDouble;
                Valor = valorPadraoDouble;
            }

            // Força atualização do Entry após a inicialização
            Action updateAction = () =>
            {
                var entry = this.FindByName<Entry>("SfNumericTextBox");
                if (entry != null && Valor.HasValue)
                {
                    entry.Text = Valor.Value.ToString("F2");
                }
            };

            if (Dispatcher != null)
                Dispatcher.Dispatch(updateAction);
            else if (Application.Current?.Dispatcher != null)
                Application.Current.Dispatcher.Dispatch(updateAction);
            else
                updateAction();

            Debug.WriteLine($"[ValorDouble] BindingContext: {ParametroComAlternativas.nome} - ValorDouble: {ParametroComAlternativas.ValorDouble} - Valor UI: {Valor} - Respondida: {ParametroComAlternativas.EstaRespondida}");
        }

        protected override void OnContextCleared()
        {
            ParametroComAlternativas = null;
            Valor = null;
        }

        /// <summary>
        /// Texto auxiliar mostrando o intervalo de valores permitidos.
        /// </summary>
        public string HelperText
        {
            get
            {
                if (ParametroComAlternativas == null || !ShowHelperText)
                    return "";

                string min = ParametroComAlternativas.valorMinimo?.ToString("F2") ?? "∞";
                string max = ParametroComAlternativas.valorMaximo?.ToString("F2") ?? "∞";

                return $"Intervalo: {min} - {max}";
            }
        }

        /// <summary>
        /// Mostra o texto auxiliar se houver máximo definido.
        /// </summary>
        public bool ShowHelperText =>
            ParametroComAlternativas != null &&
            (ParametroComAlternativas.valorMinimo.HasValue || ParametroComAlternativas.valorMaximo.HasValue);

        /// <summary>
        /// Implementação de ICampoObrigatorio.
        /// ✅ Sincroniza e valida corretamente
        /// </summary>
        protected override bool ComputeHasError()
        {
            // ✅ Sincronização Extra: Se Valor estiver nulo mas o Entry tiver texto, força o parse.
            // Isso evita problemas se o usuário clicar em Salvar antes do Binding atualizar o Valor.
            var entry = this.FindByName<Entry>("SfNumericTextBox");
            if (!Valor.HasValue && entry != null && !string.IsNullOrWhiteSpace(entry.Text))
            {
                if (double.TryParse(entry.Text, out double parsed))
                {
                    Valor = parsed;
                    Debug.WriteLine($"[VALIDATION] Sincronização rápida em ValorDouble: Valor atualizado de '{entry.Text}' para {parsed}");
                }
            }

            // ✅ Sincroniza Valor com ParametroComAlternativas
            if (ParametroComAlternativas != null)
            {
                ParametroComAlternativas.ValorDouble = Valor;
            }

            // ✅ CORREÇÃO: 0 é um valor válido. O erro ocorre apenas se for nulo e obrigatório (respeita ReadOnly).
            bool campoVazio = !Valor.HasValue;
            bool temErro = ParametroComAlternativas?.required == 1 && campoVazio && IsAnyValidationActive && !IsReadOnly;

            if (temErro)
            {
                Debug.WriteLine($"[VALIDATION ERROR] Campo '{ParametroComAlternativas?.nome}' (ValorDouble) é obrigatório mas está vazio.");
            }

            return temErro;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            var titleLabel = this.FindByName<Label>("labelTitle");
            var border = this.FindByName<Border>("valorDoubleBorder");

            ValidationVisualHelper.ApplyTitleColor(titleLabel, hasError);

            if (border != null)
            {
                border.Stroke = hasError ? Colors.Red : ValidationVisualHelper.GetPrimaryColor();
                border.StrokeThickness = hasError ? 2 : 1;
            }
        }

        /// <summary>
        /// Handler para quando o Entry perde o foco.
        /// Valida e formata o valor.
        /// </summary>
        private void Entry_Unfocused(object sender, FocusEventArgs e)
        {
            if (sender is not Entry entry || ParametroComAlternativas == null)
                return;

            if (string.IsNullOrWhiteSpace(entry.Text))
            {
                Valor = null;
                entry.Text = string.Empty;
                return;
            }

            // ✅ Tenta fazer parse do valor
            if (double.TryParse(entry.Text, out double parsedValue))
            {
                // ✅ Aplicar limites
                if (ParametroComAlternativas.valorMinimo.HasValue && parsedValue < ParametroComAlternativas.valorMinimo)
                    parsedValue = (double)ParametroComAlternativas.valorMinimo.Value;

                if (ParametroComAlternativas.valorMaximo.HasValue && parsedValue > ParametroComAlternativas.valorMaximo)
                    parsedValue = (double)ParametroComAlternativas.valorMaximo.Value;

                Valor = parsedValue;
                entry.Text = parsedValue.ToString("F2");

                // ✅ SINCRONIZA
                ParametroComAlternativas.ValorDouble = parsedValue;

                Debug.WriteLine($"[ValorDouble] Valor formatado: {parsedValue:F2} - Respondida: {ParametroComAlternativas.EstaRespondida}");
            }
            else
            {
                // ✅ Valor inválido - limpar
                Valor = null;
                entry.Text = string.Empty;

                ParametroComAlternativas.ValorDouble = null;

                Debug.WriteLine($"[ValorDouble] Valor inválido - Respondida: false");
            }
        }
    }
}
