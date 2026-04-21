using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Models;
using Microsoft.Maui.Controls;
using Syncfusion.Maui.Inputs;
using System.Diagnostics;

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

        private static void OnIsReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ComboAlternativas control)
            {
                control.OnPropertyChanged(nameof(NotIsReadOnly));
                control.ScheduleValidationRefresh();
                Debug.WriteLine($"[ComboAlternativas] IsReadOnly mudou: {newValue}");
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
                Debug.WriteLine($"[CRASH_INICIALIZACAO] ComboAlternativas: {ex.Message}");
                Exception? inner = ex.InnerException;
                while (inner != null)
                {
                    Debug.WriteLine($"[CRASH_INNER] {inner.Message}");
                    Debug.WriteLine($"[CRASH_TRACE] {inner.StackTrace}");
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
            {
                ComboBoxProdutoNomeTratamento = null;
            }

            ClearComboUi();
        }

        private async Task OnContextAttachedInternalAsync(int currentVersion)
        {
            var parametro = BindingContext as ParametroComAlternativas;
            ParametroComAlternativas = parametro;

            Debug.WriteLine($"[ComboAlternativas] ═══════════════════════════════════════");
            Debug.WriteLine($"[ComboAlternativas] OnContextAttached CHAMADO");
            Debug.WriteLine($"[ComboAlternativas]   Novo:   {ParametroComAlternativas?.nome ?? "NULL"}");
            Debug.WriteLine($"[ComboAlternativas] ═══════════════════════════════════════");

            if (parametro == null || currentVersion != _bindingContextVersion)
            {
                return;
            }

            try
            {
                Debug.WriteLine($"[ComboAlternativas] → Processando modelo:");
                Debug.WriteLine($"[ComboAlternativas]     Nome: {parametro.nome}");
                Debug.WriteLine($"[ComboAlternativas]     ID: {parametro.id}");
                Debug.WriteLine($"[ComboAlternativas]     SelectedIndex: {parametro.SelectedIndex}");
                Debug.WriteLine($"[ComboAlternativas]     Required: {parametro.required}");

                await UpdateProdutoNomeTratamentoReferenceAsync(currentVersion).ConfigureAwait(false);

                if (ParametroComAlternativas == null || currentVersion != _bindingContextVersion)
                {
                    return;
                }

                if (parametro.required == 1 && parametro.SelectedIndex < 0)
                {
                    var idxPadrao = parametro.ListaAlternativas?.FindIndex(a => a.valorPadrao == 1) ?? -1;
                    if (idxPadrao >= 0)
                    {
                        Debug.WriteLine($"[ComboAlternativas] → Aplicando valor padrão: Index={idxPadrao}");
                        parametro.SelectedIndex = idxPadrao;
                    }
                }

                await RestoreSelectionFromModelAsync(currentVersion).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComboAlternativas] ✖ ERRO em OnContextAttached:");
                Debug.WriteLine($"[ComboAlternativas]   {ex.Message}");
                Debug.WriteLine($"[ComboAlternativas]   {ex.StackTrace}");
            }
            finally
            {
                if (currentVersion == _bindingContextVersion)
                {
                    _isRestoring = false;
                    if (IsAnyValidationActive || _isValidationActive)
                    {
                        ScheduleValidationRefresh();
                    }
                }

                Debug.WriteLine($"[ComboAlternativas] → OnContextAttached concluído (validação={_isValidationActive})");
            }
        }

        private async Task RestoreSelectionFromModelAsync(int expectedVersion)
        {
            if (ParametroComAlternativas == null || _isDisposed || expectedVersion != _bindingContextVersion)
            {
                Debug.WriteLine($"[ComboAlternativas] RestoreSelection ABORTADO: Modelo NULL/Disposed/Versão antiga");
                return;
            }

            Debug.WriteLine($"[ComboAlternativas] ┌─────────────────────────────────────");
            Debug.WriteLine($"[ComboAlternativas] │ ★ INICIANDO RESTAURAÇÃO DE ESTADO ★");
            Debug.WriteLine($"[ComboAlternativas] └─────────────────────────────────────");

            try
            {
                await Task.Delay(50).ConfigureAwait(false);

                if (ParametroComAlternativas == null || _isDisposed || expectedVersion != _bindingContextVersion)
                {
                    Debug.WriteLine($"[ComboAlternativas] RestoreSelection ABORTADO: Estado mudou durante delay");
                    return;
                }

                var targetIndex = ParametroComAlternativas.SelectedIndex;
                var targetItem = ParametroComAlternativas.AlternativaSelecionada;
                var listaAlternativas = ParametroComAlternativas.ListaAlternativas;

                Debug.WriteLine($"[ComboAlternativas] │ Dados do modelo:");
                Debug.WriteLine($"[ComboAlternativas] │   Nome: {ParametroComAlternativas.nome}");
                Debug.WriteLine($"[ComboAlternativas] │   SelectedIndex: {targetIndex}");
                Debug.WriteLine($"[ComboAlternativas] │   Lista: {listaAlternativas?.Count ?? 0} itens");
                Debug.WriteLine($"[ComboAlternativas] │   Item: {targetItem?.descricao ?? "NULL"}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_isDisposed || ParametroComAlternativas == null || expectedVersion != _bindingContextVersion)
                    {
                        Debug.WriteLine($"[ComboAlternativas] RestoreSelection ABORTADO: Estado inválido na UI thread");
                        return;
                    }

                    Debug.WriteLine($"[ComboAlternativas] │");
                    Debug.WriteLine($"[ComboAlternativas] │ ★★★ APLICANDO NA UI ★★★");

                    ComboBox.ItemsSource = listaAlternativas;
                    Debug.WriteLine($"[ComboAlternativas] │   ✓ ItemsSource aplicado ({listaAlternativas?.Count ?? 0} itens)");

                    if (targetIndex >= 0 && targetItem != null)
                    {
                        ComboBox.SelectedItem = targetItem;
                        ComboBox.SelectedIndex = targetIndex;
                        Debug.WriteLine($"[ComboAlternativas] │   ✓ Seleção aplicada: Index={targetIndex}, Item={targetItem.descricao}");
                    }
                    else
                    {
                        ComboBox.SelectedItem = null;
                        ComboBox.SelectedIndex = -1;
                        ComboBox.Text = string.Empty;
                        Debug.WriteLine($"[ComboAlternativas] │   ✓ Combo limpo (sem seleção)");
                    }
                });

                Debug.WriteLine($"[ComboAlternativas] │");
                Debug.WriteLine($"[ComboAlternativas] └─ ★ RESTAURAÇÃO CONCLUÍDA COM SUCESSO ★");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComboAlternativas] ✖✖✖ ERRO NA RESTAURAÇÃO:");
                Debug.WriteLine($"[ComboAlternativas]   {ex.Message}");
                Debug.WriteLine($"[ComboAlternativas]   {ex.StackTrace}");
            }
        }

        private async Task UpdateProdutoNomeTratamentoReferenceAsync(int expectedVersion)
        {
            if (expectedVersion != _bindingContextVersion || ParametroComAlternativas == null)
            {
                return;
            }

            var config = await ConfigParametros.Config().ConfigureAwait(false);

            if (expectedVersion != _bindingContextVersion || ParametroComAlternativas == null)
            {
                return;
            }

            if (ParametroComAlternativas.id == config.parametroTratamentoNomeProdutoId)
            {
                ComboBoxProdutoNomeTratamento = this;
                Debug.WriteLine($"[ComboAlternativas] → Registrado como ComboBoxProdutoNomeTratamento");
            }
            else if (ComboBoxProdutoNomeTratamento == this)
            {
                ComboBoxProdutoNomeTratamento = null;
                Debug.WriteLine($"[ComboAlternativas] → Referência ComboBoxProdutoNomeTratamento limpa");
            }
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
            _ = OnComboSelectionChangedInternalAsync();
        }

        private async Task OnComboSelectionChangedInternalAsync()
        {
            if (_isRestoring)
            {
                Debug.WriteLine($"[ComboAlternativas] SelectionChanged IGNORADO: _isRestoring=true");
                return;
            }

            if (ParametroComAlternativas == null)
            {
                Debug.WriteLine($"[ComboAlternativas] SelectionChanged IGNORADO: Modelo NULL");
                return;
            }

            _isValidationActive = true;

            var newIndex = ComboBox.SelectedIndex;

            if (newIndex == -1 && !_isRestoring && ParametroComAlternativas.SelectedIndex >= 0)
            {
                Debug.WriteLine($"[ComboAlternativas] AVISO: Tentativa de reset UI para -1 ignorada. Mantendo modelo={ParametroComAlternativas.SelectedIndex}.");
                return;
            }

            Debug.WriteLine($"[ComboAlternativas] ★ USUÁRIO SELECIONOU:");
            Debug.WriteLine($"[ComboAlternativas]   Nome: {ParametroComAlternativas.nome}");
            Debug.WriteLine($"[ComboAlternativas]   Novo Index: {newIndex}");
            Debug.WriteLine($"[ComboAlternativas]   Antigo Index: {ParametroComAlternativas.SelectedIndex}");

            if (ParametroComAlternativas.SelectedIndex != newIndex)
            {
                ParametroComAlternativas.SelectedIndex = newIndex;
                Debug.WriteLine($"[ComboAlternativas]   ✓ Modelo atualizado");
            }

            ScheduleValidationRefresh();
            await UpdateProdutoDependenteAsync().ConfigureAwait(false);
        }

        private async Task UpdateProdutoDependenteAsync()
        {
            if (_updatingDependency || ParametroComAlternativas == null)
            {
                return;
            }

            try
            {
                var config = await ConfigParametros.Config().ConfigureAwait(false);
                if (ParametroComAlternativas.id != config.parametroTratamentoProdutoId)
                {
                    return;
                }

                Debug.WriteLine($"[ComboAlternativas] UpdateDependente: Iniciando");

                var destino = ComboBoxProdutoNomeTratamento;
                if (destino?.ParametroComAlternativas == null)
                {
                    Debug.WriteLine($"[ComboAlternativas] UpdateDependente: Destino inválido");
                    return;
                }

                var alternativaSelecionada = ParametroComAlternativas.AlternativaSelecionada;
                if (alternativaSelecionada == null)
                {
                    Debug.WriteLine($"[ComboAlternativas] UpdateDependente: Sem alternativa selecionada");
                    return;
                }

                _updatingDependency = true;

                var novasAlternativas = await ParametroAlternativas
                    .PegaAlternativasDiagnosticoTratamento(alternativaSelecionada.id)
                    .ConfigureAwait(false);

                Debug.WriteLine($"[ComboAlternativas] UpdateDependente: Recebidas {novasAlternativas?.Count ?? 0} alternativas");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (destino._isDisposed || destino.ParametroComAlternativas == null)
                    {
                        return;
                    }

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

                    Debug.WriteLine($"[ComboAlternativas] UpdateDependente: Concluído");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComboAlternativas] UpdateDependente ERRO: {ex.Message}");
            }
            finally
            {
                _updatingDependency = false;
            }
        }

        new public bool PreenchidoCorretamente()
        {
            if (!_isRestoring && ParametroComAlternativas != null && ComboBox.SelectedIndex >= 0 && ParametroComAlternativas.SelectedIndex == -1)
            {
                Debug.WriteLine($"[ComboAlternativas] PreenchidoCorretamente: Forçando sincronização UI({ComboBox.SelectedIndex}) -> Modelo.");
                ParametroComAlternativas.SelectedIndex = ComboBox.SelectedIndex;
            }

            _isValidationActive = true;
            return base.PreenchidoCorretamente();
        }

        protected override bool ComputeHasError()
        {
            if (!_isRestoring && ParametroComAlternativas != null && ComboBox.SelectedIndex >= 0 && ParametroComAlternativas.SelectedIndex == -1)
            {
                ParametroComAlternativas.SelectedIndex = ComboBox.SelectedIndex;
            }

            bool selecionado = (ParametroComAlternativas?.SelectedIndex ?? -1) >= 0 || ComboBox.SelectedIndex >= 0;
            bool obrigatorio = ParametroComAlternativas?.required == 1;
            bool temErro = obrigatorio && !selecionado && NotIsReadOnly && !_isRestoring && (IsAnyValidationActive || _isValidationActive);

            Debug.WriteLine($"[ComboAlternativas] Validação:");
            Debug.WriteLine($"[ComboAlternativas]   Nome: {ParametroComAlternativas?.nome}");
            Debug.WriteLine($"[ComboAlternativas]   Obrigatório: {obrigatorio}");
            Debug.WriteLine($"[ComboAlternativas]   Selecionado: {selecionado}");
            Debug.WriteLine($"[ComboAlternativas]   ReadOnly: {IsReadOnly}");
            Debug.WriteLine($"[ComboAlternativas]   TEM ERRO VISÍVEL: {temErro}");

            return temErro;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            ValidationVisualHelper.ApplyTitleColor(labelTitle, hasError);
        }

        private void OpenComboBox(object sender, EventArgs e)
        {
            if (NotIsReadOnly && !_isDisposed)
            {
                ComboBox.Focus();
                Debug.WriteLine($"[ComboAlternativas] ComboBox aberto via tap");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Debug.WriteLine($"[ComboAlternativas] ═══ Dispose: {ParametroComAlternativas?.nome} ═══");

            _isDisposed = true;

            ComboBox.SelectionChanged -= OnComboSelectionChanged;

            if (ComboBoxProdutoNomeTratamento == this)
            {
                ComboBoxProdutoNomeTratamento = null;
            }

            ParametroComAlternativas = null;
            _isValidationActive = false;

            Debug.WriteLine($"[ComboAlternativas] ═══ Dispose concluído ═══");
        }
    }
}
