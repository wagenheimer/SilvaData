using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Infrastructure;
using SilvaData.Models;


using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

using SelectionChangedEventArgs = Syncfusion.Maui.Inputs.SelectionChangedEventArgs;

namespace SilvaData.Controls
{
    /// <summary>
    /// ComboBox para selecionar Propriedade. Usa CacheService e implementa ICampoObrigatorio.
    /// </summary>
    public partial class PropriedadeComboBox : ValidatableFieldBase
    {
        private readonly CacheService _cacheService;

        /// <summary>Previne loops infinitos de sincronização (ViewModel → View → ViewModel)</summary>
        private bool _isSyncingSelection;

        // ============================================================================
        // CICLO DE VIDA
        // ============================================================================

        public PropriedadeComboBox()
        {
            _cacheService = ServiceHelper.GetRequiredService<CacheService>();
            InitializeComponent();

            this.Loaded += OnComboBoxLoaded;
            this.Unloaded += OnComboBoxUnloaded;
        }

        /// <summary>Registra listeners de mensagens quando o controle carrega</summary>
        private void OnComboBoxLoaded(object? sender, EventArgs e)
        {
            try
            {
                // Atualiza dados quando página inicia
                WeakReferenceMessenger.Default.Register<UpdateDadosIniciaisMessage>(
                    this, async (r, m) => await AtualizaDados());

                // Atualiza quando cache é sincronizado
                WeakReferenceMessenger.Default.Register<RefreshCacheMessage>(this, async (r, m) =>
                {
                    if (m.Type == CacheType.Propriedades || m.Type == CacheType.All)
                        await AtualizaDados();
                });

                // Seleciona automaticamente quando nova Propriedade é adicionada
                WeakReferenceMessenger.Default.Register<PropriedadeAdicionadaMessage>(
                    this, async (r, m) => await PropriedadeAdicionada(m.Propriedade.id));

                Debug.WriteLine("[PropriedadeComboBox] Carregado");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PropriedadeComboBox] Erro: {ex.Message}");
            }
        }

        /// <summary>Desregistra listeners quando o controle descarrega (evita memory leaks)</summary>
        private void OnComboBoxUnloaded(object? sender, EventArgs e)
        {
            try
            {
                WeakReferenceMessenger.Default.Unregister<UpdateDadosIniciaisMessage>(this);
                WeakReferenceMessenger.Default.Unregister<RefreshCacheMessage>(this);
                WeakReferenceMessenger.Default.Unregister<PropriedadeAdicionadaMessage>(this);

                Debug.WriteLine("[PropriedadeComboBox] Descarregado");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PropriedadeComboBox] Erro: {ex.Message}");
            }
        }

        // ============================================================================
        // PROPRIEDADES PÚBLICAS
        // ============================================================================

        /// <summary>Lista de Propriedades do cache</summary>
        public ObservableCollection<Propriedade> PropriedadeList => _cacheService.PropriedadeList;

        public bool EstaSelecionado => propriedadecombobox?.SelectedItem != null;
        public bool PrecisaMostrarApagar => propriedadecombobox?.SelectedItem != null;
        public bool TemMaisdeUma => PropriedadeList?.Count > 1;
        public int TrailingViewWidthRequest => PrecisaMostrarApagar ? 60 : 0;

        // ============================================================================
        // MÉTODOS PÚBLICOS
        // ============================================================================

        /// <summary>Atualiza lista e limpa seleção</summary>
        public async Task AtualizaDados()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ClearSelected(null, EventArgs.Empty);
                OnPropertyChanged(nameof(PropriedadeList));
                propriedadecombobox.ItemsSource = PropriedadeList;
                Debug.WriteLine("[PropriedadeComboBox] Dados atualizados");
            });
        }

        /// <summary>Seleciona automaticamente se houver apenas um item</summary>
        public void CheckJustOne()
        {
            if (PropriedadeList?.Count == 1)
                SelectedItem = PropriedadeList[0];

            OnPropertyChanged(nameof(TemMaisdeUma));
        }

        // ============================================================================
        // COMANDOS E EVENTOS
        // ============================================================================

        /// <summary>Abre página para adicionar nova Propriedade</summary>
        [RelayCommand(CanExecute = nameof(CanExecuteAddNewComboBox))]
        private async Task AddNewComboBoxAsync()
        {
            HapticHelper.VibrateClick();
            await NavigationUtils.ShowViewAsModalAsync<PropriedadeView_Edit>();
        }

        private bool CanExecuteAddNewComboBox()
            => Permissoes.UsuarioPermissoes?.propriedades.cadastrar ?? false;

        private void ClearSelected(object? sender, EventArgs e)
        {
            SelectedItem = null;
            Debug.WriteLine("[PropriedadeComboBox] Seleção limpa");
        }

        /// <summary>Fired quando usuário seleciona no ComboBox</summary>
        private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return;

            var selectedItem = e.AddedItems.Count > 0 ? e.AddedItems[0] as Propriedade : null;
            if (!Equals(SelectedItem, selectedItem))
            {
                SelectedItem = selectedItem;
            }

            Command?.Execute(null);
            NotifyChanged();
            ScheduleValidationRefresh();
            Debug.WriteLine($"[PropriedadeComboBox] Selecionado: {selectedItem?.nome}");
        }

        /// <summary>Seleciona automaticamente Propriedade recém-adicionada</summary>
        public async Task PropriedadeAdicionada(int? propriedadeId)
        {
            await AtualizaDados();
            SelectedItem = _cacheService.PropriedadeList.FirstOrDefault(prop => prop.id == propriedadeId);
            Debug.WriteLine($"[PropriedadeComboBox] Nova Propriedade: {propriedadeId}");
        }

        /// <summary>Notifica UI sobre mudanças nas propriedades calculadas</summary>
        public void NotifyChanged()
        {
            OnPropertyChanged(nameof(EstaSelecionado));
            OnPropertyChanged(nameof(SelectedItem));
            OnPropertyChanged(nameof(TrailingViewWidthRequest));
            OnPropertyChanged(nameof(PrecisaMostrarApagar));
            AddNewComboBoxCommand.NotifyCanExecuteChanged();
        }

        private void ShowPopUp(object sender, EventArgs e)
        {
            if (!IsReadOnly)
                propriedadecombobox.Focus();
        }

        protected override bool ComputeHasError()
        {
            bool temErro = IsObrigatorio && SelectedItem == null && IsAnyValidationActive && !IsReadOnly;
            Debug.WriteLine($"[PropriedadeComboBox] Validação: Erro={temErro}");
            return temErro;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            var titleLabel = this.FindByName<Label>("labelTitle");
            var border = this.FindByName<Border>("propriedadeBorder");

            ValidationVisualHelper.ApplyTitleColor(titleLabel, hasError);

            if (border != null)
            {
                border.Stroke = hasError ? Colors.Red : ValidationVisualHelper.GetPrimaryColor();
                border.StrokeThickness = hasError ? 2 : 1;
            }
        }

        // ============================================================================
        // BINDABLE PROPERTIES
        // ============================================================================

        /// <summary>Propriedade selecionada (sincronização bidirecional ViewModel ↔ View)</summary>
        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(
                nameof(SelectedItem),
                typeof(Propriedade),
                typeof(PropriedadeComboBox),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnSelectedItemChanged);

        /// <summary>Callback: quando ViewModel define SelectedItem, atualiza UI</summary>
        private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (PropriedadeComboBox)bindable;
            var prop = newValue as Propriedade;

            if (Equals(control.propriedadecombobox?.SelectedItem, prop))
            {
                control.NotifyChanged();
                control.ScheduleValidationRefresh();
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    control._isSyncingSelection = true;

                    if (prop == null)
                    {
                        control.propriedadecombobox.SelectedItem = null;
                    }
                    else
                    {
                        // Busca item na lista (evita referência diferente do mesmo objeto)
                        var match = control.PropriedadeList?.FirstOrDefault(i => i?.id == prop.id);
                        control.propriedadecombobox.SelectedItem = match;
                    }
                }
                finally
                {
                    control._isSyncingSelection = false; // Libera flag de sincronização
                }

                control.NotifyChanged();
                control.ScheduleValidationRefresh();
            });
        }

        public Propriedade? SelectedItem
        {
            get => (Propriedade?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>Comando executado quando seleção muda</summary>
        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(
                nameof(Command),
                typeof(ICommand),
                typeof(PropriedadeComboBox),
                null);

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>Modo somente leitura: desabilita ComboBox</summary>
        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(PropriedadeComboBox),
                false,
                BindingMode.TwoWay,
                propertyChanged: ReadOnlyChanged);

        private static void ReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is PropriedadeComboBox control)
            {
                control.OnPropertyChanged(nameof(IsReadOnly));
                control.OnPropertyChanged(nameof(NotIsReadOnly));
                control.ScheduleValidationRefresh();
                Debug.WriteLine($"[PropriedadeComboBox] IsReadOnly={newValue}");
            }
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool NotIsReadOnly => !IsReadOnly;

        /// <summary>Define se o campo é obrigatório</summary>
        public static readonly BindableProperty IsObrigatorioProperty =
            BindableProperty.Create(nameof(IsObrigatorio), typeof(bool), typeof(PropriedadeComboBox), false);

        public bool IsObrigatorio
        {
            get => (bool)GetValue(IsObrigatorioProperty);
            set
            {
                SetValue(IsObrigatorioProperty, value);
                ScheduleValidationRefresh();
            }
        }
    }
}
