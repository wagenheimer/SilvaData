namespace SilvaData.Controls
{
    /// <summary>
    /// Combo com alternativas que preserva a seleção durante reciclagem de células.
    /// </summary>
    public partial class ComboAlternativas : ValidatableFieldBase, IDisposable
    {
        private static WeakReference<ComboAlternativas>? _comboBoxProdutoNomeTratamentoRef;

        private bool _isDisposed;
        private bool _updatingDependency;
        private bool _isRestoring;
        private bool _isValidationActive;
        private int _bindingContextVersion;

        public static ComboAlternativas? ComboBoxProdutoNomeTratamento
        {
            get => _comboBoxProdutoNomeTratamentoRef?.TryGetTarget(out var target) == true ? target : null;
            set => _comboBoxProdutoNomeTratamentoRef = value == null ? null : new WeakReference<ComboAlternativas>(value);
        }

        public ParametroComAlternativas? ParametroComAlternativas { get; private set; }

        public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(ComboAlternativas),
            false,
            BindingMode.TwoWay,
            propertyChanged: OnIsReadOnlyChanged);

        private static void OnIsReadOnlyChanged(BindableObject bindable, object oldValue, object _)
        {
            if (bindable is ComboAlternativas control)
            {
                control.OnPropertyChanged(nameof(NotIsReadOnly));
                control.ScheduleValidationRefresh();
            }
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool NotIsReadOnly => !IsReadOnly;

        public ComboAlternativas()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRASH_INICIALIZACAO] ComboAlternativas: {ex.Message}");
                Exception? inner = ex.InnerException;
                while (inner != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CRASH_INNER] {inner.Message}");
                    System.Diagnostics.Debug.WriteLine($"[CRASH_TRACE] {inner.StackTrace}");
                    inner = inner.InnerException;
                }
                throw;
            }

            ComboBox.SelectionChanged += OnComboSelectionChanged;
        }

        protected override void OnContextAttached()
        {
            _isRestoring = true;
            _ = OnContextAttachedInternalAsync(++_bindingContextVersion);
        }

        protected override void OnContextCleared()
        {
            _bindingContextVersion++;
            _isValidationActive = false;
            ParametroComAlternativas = null;

            if (ComboBoxProdutoNomeTratamento == this)
                ComboBoxProdutoNomeTratamento = null;

            ClearComboUi();
        }

        private async Task OnContextAttachedInternalAsync(int currentVersion)
        {
            var parametro = BindingContext as ParametroComAlternativas;
            ParametroComAlternativas = parametro;

            if (parametro == null || currentVersion != _bindingContextVersion)
                return;

            try
            {
                await UpdateProdutoNomeTratamentoReferenceAsync(currentVersion).ConfigureAwait(false);

                if (ParametroComAlternativas == null || currentVersion != _bindingContextVersion)
                    return;

                if (parametro.required == 1 && parametro.SelectedIndex < 0)
                {
                    var idxPadrao = parametro.ListaAlternativas?.FindIndex(a => a.valorPadrao == 1) ?? -1;
                    if (idxPadrao >= 0)
                        parametro.SelectedIndex = idxPadrao;
                }

                await RestoreSelectionFromModelAsync(currentVersion).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComboAlternativas] ERRO em OnContextAttached: {ex.Message}");
            }
            finally
            {
                if (currentVersion == _bindingContextVersion)
                {
                    _isRestoring = false;
                    if (IsAnyValidationActive || _isValidationActive)
                        ScheduleValidationRefresh();
                    OnPropertyChanged(nameof(ShowRequiredStar));
                }
            }
        }

        private async Task RestoreSelectionFromModelAsync(int expectedVersion)
        {
            if (ParametroComAlternativas == null || _isDisposed || expectedVersion != _bindingContextVersion)
                return;

            try
            {
                await Task.Delay(50).ConfigureAwait(false);

                if (ParametroComAlternativas == null || _isDisposed || expectedVersion != _bindingContextVersion)
                    return;

                var targetIndex = ParametroComAlternativas.SelectedIndex;
                var targetItem = ParametroComAlternativas.AlternativaSelecionada;
                var listaAlternativas = ParametroComAlternativas.ListaAlternativas;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_isDisposed || ParametroComAlternativas == null || expectedVersion != _bindingContextVersion)
                        return;

                    ComboBox.ItemsSource = listaAlternativas;

                    if (targetIndex >= 0 && targetItem != null)
                    {
                        ComboBox.SelectedItem = targetItem;
                        ComboBox.SelectedIndex = targetIndex;
                    }
                    else
                    {
                        ComboBox.SelectedItem = null;
                        ComboBox.SelectedIndex = -1;
                        ComboBox.Text = string.Empty;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComboAlternativas] ERRO na restauração: {ex.Message}");
            }
        }

        private async Task UpdateProdutoNomeTratamentoReferenceAsync(int expectedVersion)
        {
            if (expectedVersion != _bindingContextVersion || ParametroComAlternativas == null)
                return;

            var config = await ConfigParametros.Config().ConfigureAwait(false);

            if (expectedVersion != _bindingContextVersion || ParametroComAlternativas == null)
                return;

            if (ParametroComAlternativas.id == config.parametroTratamentoNomeProdutoId)
                ComboBoxProdutoNomeTratamento = this;
            else if (ComboBoxProdutoNomeTratamento == this)
                ComboBoxProdutoNomeTratamento = null;
        }

        private void ClearComboUi()
        {
            _isRestoring = true;
            try
            {
                ComboBox.SelectedIndex = -1;
                ComboBox.SelectedItem = null;
                ComboBox.Text = string.Empty;
            }
            finally
            {
                _isRestoring = false;
            }
        }

        private void OnComboSelectionChanged(object? sender, EventArgs e)
        {
            if (_isRestoring || ParametroComAlternativas == null)
                return;

            _isValidationActive = true;

            var newIndex = ComboBox.SelectedIndex;

            if (newIndex == -1 && ParametroComAlternativas.SelectedIndex >= 0)
                return;

            if (ParametroComAlternativas.SelectedIndex != newIndex)
                ParametroComAlternativas.SelectedIndex = newIndex;

            OnPropertyChanged(nameof(ShowRequiredStar));
            ScheduleValidationRefresh();

            _ = UpdateProdutoDependenteAsync();
        }

        private async Task UpdateProdutoDependenteAsync()
        {
            if (_updatingDependency || ParametroComAlternativas == null)
                return;

            try
            {
                var config = await ConfigParametros.Config().ConfigureAwait(false);
                if (ParametroComAlternativas.id != config.parametroTratamentoProdutoId)
                    return;

                var destino = ComboBoxProdutoNomeTratamento;
                if (destino?.ParametroComAlternativas == null)
                    return;

                var alternativaSelecionada = ParametroComAlternativas.AlternativaSelecionada;
                if (alternativaSelecionada == null)
                    return;

                _updatingDependency = true;

                var novasAlternativas = await ParametroAlternativas
                    .PegaAlternativasDiagnosticoTratamento(alternativaSelecionada.id)
                    .ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (destino._isDisposed || destino.ParametroComAlternativas == null)
                        return;

                    destino.ParametroComAlternativas.ListaAlternativas = novasAlternativas;
                    destino.ParametroComAlternativas.SelectedIndex = -1;

                    destino._isRestoring = true;
                    try
                    {
                        destino.ComboBox.ItemsSource = novasAlternativas;
                        destino.ComboBox.SelectedIndex = -1;
                        destino.ComboBox.SelectedItem = null;
                        destino.ComboBox.Text = string.Empty;
                    }
                    finally
                    {
                        destino._isRestoring = false;
                    }

                    destino.ScheduleValidationRefresh();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComboAlternativas] UpdateDependente ERRO: {ex.Message}");
            }
            finally
            {
                _updatingDependency = false;
            }
        }

        new public bool PreenchidoCorretamente()
        {
            _isValidationActive = true;
            return base.PreenchidoCorretamente();
        }

        protected override bool ComputeHasError()
        {
            bool selecionado = (ParametroComAlternativas?.SelectedIndex ?? -1) >= 0 || ComboBox.SelectedIndex >= 0;
            bool obrigatorio = ParametroComAlternativas?.required == 1;
            return obrigatorio && !selecionado && NotIsReadOnly && !_isRestoring && (IsAnyValidationActive || _isValidationActive);
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            ValidationVisualHelper.ApplyTitleColor(labelTitle, hasError);
        }

        public bool ShowRequiredStar => (ParametroComAlternativas?.required == 1) && (ParametroComAlternativas?.SelectedIndex < 0);

        private void OpenComboBox(object sender, EventArgs e)
        {
            if (NotIsReadOnly && !_isDisposed)
                ComboBox.Focus();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            ComboBox.SelectionChanged -= OnComboSelectionChanged;

            if (ComboBoxProdutoNomeTratamento == this)
                ComboBoxProdutoNomeTratamento = null;

            ParametroComAlternativas = null;
            _isValidationActive = false;
        }
    }
}
