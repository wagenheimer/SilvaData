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
    /// ComboBox para selecionar Regional. Usa CacheService e implementa ICampoObrigatorio.
    /// </summary>
    public partial class RegionalComboBox : ValidatableFieldBase
    {
        private readonly CacheService _cacheService;
        private bool _isSyncingSelection;
        private int? _pendingSelectedId;

        // ============================================================================
        // CICLO DE VIDA
        // ============================================================================

        public RegionalComboBox()
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
                    if (m.Type == CacheType.Regionais || m.Type == CacheType.All)
                        await AtualizaDados();
                });

                // Seleciona automaticamente quando nova Regional é adicionada
                WeakReferenceMessenger.Default.Register<RegionalAdicionadaMessage>(
                    this, async (r, m) => await RegionalAdicionada(m.Regional.id));

                Debug.WriteLine("[RegionalComboBox] Carregado");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RegionalComboBox] Erro: {ex.Message}");
            }
        }

        /// <summary>Desregistra listeners quando o controle descarrega (evita memory leaks)</summary>
        private void OnComboBoxUnloaded(object? sender, EventArgs e)
        {
            try
            {
                WeakReferenceMessenger.Default.Unregister<UpdateDadosIniciaisMessage>(this);
                WeakReferenceMessenger.Default.Unregister<RefreshCacheMessage>(this);
                WeakReferenceMessenger.Default.Unregister<RegionalAdicionadaMessage>(this);

                Debug.WriteLine("[RegionalComboBox] Descarregado");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RegionalComboBox] Erro: {ex.Message}");
            }
        }

        // ============================================================================
        // PROPRIEDADES PÚBLICAS
        // ============================================================================

        /// <summary>Lista de Regionais do cache</summary>
        public ObservableCollection<Regional> RegionalList => _cacheService.RegionalList;

        public bool EstaSelecionado => regionalcombobox?.SelectedItem != null;
        public bool PrecisaMostrarApagar => regionalcombobox?.SelectedItem != null;
        public bool TemMaisdeUma => RegionalList?.Count > 1;
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
                OnPropertyChanged(nameof(RegionalList));
                regionalcombobox.ItemsSource = RegionalList;

                // Tenta aplicar seleção pendente (vinha de outro lugar)
                if (_pendingSelectedId.HasValue)
                {
                    var match = RegionalList?.FirstOrDefault(i => i?.id == _pendingSelectedId);
                    if (match != null)
                    {
                        SelectedItem = match;
                        _pendingSelectedId = null;
                    }
                }

                Debug.WriteLine("[RegionalComboBox] Dados atualizados");
            });
        }

        /// <summary>Seleciona automaticamente se houver apenas um item</summary>
        public void CheckJustOne()
        {
            if (RegionalList?.Count == 1)
                SelectedItem = RegionalList[0];

            OnPropertyChanged(nameof(TemMaisdeUma));
        }

        // ============================================================================
        // COMANDOS E EVENTOS
        // ============================================================================

        /// <summary>Abre página para adicionar nova Regional</summary>
        [RelayCommand(CanExecute = nameof(CanExecuteAddNewComboBox))]
        private async Task AddNewComboBoxAsync()
        {
            HapticHelper.VibrateClick();
            await NavigationUtils.ShowViewAsModalAsync<RegionalView_Edit>();
        }

        private bool CanExecuteAddNewComboBox()
            => Permissoes.UsuarioPermissoes?.regionais.cadastrar ?? false;

        private void ClearSelected(object? sender, EventArgs e)
        {
            SelectedItem = null;
            Debug.WriteLine("[RegionalComboBox] Seleção limpa");
        }

        /// <summary>Disparado quando usuário seleciona no ComboBox</summary>
        private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return;

            var selectedItem = e.AddedItems.Count > 0 ? e.AddedItems[0] as Regional : null;
            if (!Equals(SelectedItem, selectedItem))
            {
                SelectedItem = selectedItem;
            }

            Command?.Execute(null);
            Debug.WriteLine($"[RegionalComboBox] Selecionado: {selectedItem?.nome}");
        }

        /// <summary>Seleciona automaticamente Regional recém-adicionada</summary>
        public async Task RegionalAdicionada(int? regionalId)
        {
            await AtualizaDados();
            SelectedItem = _cacheService.RegionalList.FirstOrDefault(reg => reg.id == regionalId);
            Debug.WriteLine($"[RegionalComboBox] Nova Regional: {regionalId}");
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
                regionalcombobox.Focus();
        }

        protected override bool ComputeHasError()
        {
            bool temErro = IsObrigatorio && SelectedItem == null && IsAnyValidationActive && !IsReadOnly;
            Debug.WriteLine($"[RegionalComboBox] Validação: Erro={temErro}");
            return temErro;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
        }

        // ============================================================================
        // BINDABLE PROPERTIES
        // ============================================================================

        /// <summary>Regional selecionada (sincronização bidirecional)</summary>
        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(
                nameof(SelectedItem),
                typeof(Regional),
                typeof(RegionalComboBox),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnSelectedItemChanged);

        /// <summary>Callback: quando ViewModel define SelectedItem, atualiza UI</summary>
        private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (RegionalComboBox)bindable;
            var regional = newValue as Regional;

            control.OnPropertyChanged(nameof(ShowRequiredStar));

            if (Equals(control.regionalcombobox?.SelectedItem, regional))
            {
                control.NotifyChanged();
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    control._isSyncingSelection = true;

                    if (regional == null)
                    {
                        control._pendingSelectedId = null;
                        control.regionalcombobox.SelectedItem = null;
                    }
                    else
                    {
                        var match = control.RegionalList?.FirstOrDefault(i => i?.id == regional.id);
                        if (match != null)
                        {
                            control._pendingSelectedId = null;
                            control.regionalcombobox.SelectedItem = match;
                        }
                        else
                        {
                            // Lista ainda não contém este item, guarda para depois
                            control._pendingSelectedId = regional.id;
                        }
                    }
                }
                finally
                {
                    control._isSyncingSelection = false;
                }
            });
        }

        public Regional? SelectedItem
        {
            get => (Regional?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>Comando executado quando seleção muda</summary>
        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(
                nameof(Command),
                typeof(ICommand),
                typeof(RegionalComboBox),
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
                typeof(RegionalComboBox),
                false,
                BindingMode.TwoWay,
                propertyChanged: ReadOnlyChanged);

        private static void ReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is RegionalComboBox control)
            {
                control.OnPropertyChanged(nameof(IsReadOnly));
                control.OnPropertyChanged(nameof(NotIsReadOnly));
                control.ScheduleValidationRefresh();
                Debug.WriteLine($"[RegionalComboBox] IsReadOnly={newValue}");
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
            BindableProperty.Create(
                nameof(IsObrigatorio), 
                typeof(bool), 
                typeof(RegionalComboBox), 
                false,
                propertyChanged: (bindable, oldValue, newValue) => 
                {
                    if (bindable is RegionalComboBox control)
                    {
                        control.OnPropertyChanged(nameof(ShowRequiredStar));
                    }
                });

        public bool IsObrigatorio
        {
            get => (bool)GetValue(IsObrigatorioProperty);
            set
            {
                SetValue(IsObrigatorioProperty, value);
                OnPropertyChanged(nameof(ShowRequiredStar));
                ScheduleValidationRefresh();
            }
        }

        /// <summary>
        /// Define se o asterisco de campo obrigatório deve ser exibido.
        /// Visível apenas se for obrigatório E ainda não estiver preenchido.
        /// </summary>
        public bool ShowRequiredStar => IsObrigatorio && SelectedItem == null;

    }
}
