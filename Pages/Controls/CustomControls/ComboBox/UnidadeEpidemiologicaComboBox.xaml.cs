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
    /// ComboBox customizado para selecionar Unidade Epidemiológica.
    /// Usa CacheService, suporta filtro por Propriedade, e implementa ICampoObrigatorio.
    /// </summary>
    public partial class UnidadeEpidemiologicaComboBox : ValidatableFieldBase
    {
        private readonly CacheService _cacheService;
        private Propriedade? _propriedadeFiltrada;

        public UnidadeEpidemiologicaComboBox()
        {
            try
            {
                _cacheService = ServiceHelper.GetRequiredService<CacheService>();
                InitializeComponent();

                this.Loaded += OnComboBoxLoaded;
                this.Unloaded += OnComboBoxUnloaded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] UnidadeEpidemiologicaComboBox: {ex.Message}");
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

        #region Ciclo de Vida

        private void OnComboBoxLoaded(object? sender, EventArgs e)
        {
            try
            {
                WeakReferenceMessenger.Default.Register<UpdateDadosIniciaisMessage>(this, async (r, m) => await AtualizaDados());
                WeakReferenceMessenger.Default.Register<RefreshCacheMessage>(this, async (r, m) =>
                {
                    if (m.Type == CacheType.UnidadesEpidemiologicas || m.Type == CacheType.All)
                    {
                        await AtualizaDados();
                    }
                });
                WeakReferenceMessenger.Default.Register<UEAdicionadaMessage>(this, async (r, m) =>
                    await UnidadeEpidemiologicaAdicionada(m.UnidadeEpidemiologica.id));

                if (unidadeEpidemiologicaCombobox != null)
                    unidadeEpidemiologicaCombobox.SelectedItem = SelectedItem;

                Debug.WriteLine("[UnidadeEpidemiologicaComboBox] Carregado");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UnidadeEpidemiologicaComboBox] Erro ao carregar: {ex.Message}");
            }
        }

        private void OnComboBoxUnloaded(object? sender, EventArgs e)
        {
            try
            {
                WeakReferenceMessenger.Default.Unregister<UpdateDadosIniciaisMessage>(this);
                WeakReferenceMessenger.Default.Unregister<RefreshCacheMessage>(this);
                WeakReferenceMessenger.Default.Unregister<UEAdicionadaMessage>(this);

                Debug.WriteLine("[UnidadeEpidemiologicaComboBox] Descarregado");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UnidadeEpidemiologicaComboBox] Erro ao descarregar: {ex.Message}");
            }
        }

        #endregion

        #region Propriedades da UI

        public ObservableCollection<UnidadeEpidemiologicaComDetalhes> UnidadeEpidemiologicaList => _cacheService.UEList;
        public bool EstaSelecionado => SelectedItem != null;
        public int TrailingViewWidthRequest => EstaSelecionado ? 60 : 0;
        public bool PrecisaMostrarApagar => !IsReadOnly && SelectedItem != null;
        public bool TemMaisdeUma => UnidadeEpidemiologicaList.Count > 1;

        #endregion

        #region Métodos Públicos

        public async Task AtualizaDados()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                SelectedItem = null;
                OnPropertyChanged(nameof(UnidadeEpidemiologicaList));
                OnPropertyChanged(nameof(TemMaisdeUma));
                if (unidadeEpidemiologicaCombobox != null)
                    unidadeEpidemiologicaCombobox.ItemsSource = UnidadeEpidemiologicaList;

                Debug.WriteLine("[UnidadeEpidemiologicaComboBox] Dados atualizados");
            });
        }

        /// <summary>
        /// Filtra a lista de UEs por Propriedade.
        /// </summary>
        public void SetFilterByPropriedade(Propriedade? value)
        {
            if (_propriedadeFiltrada == value)
                return;

            _propriedadeFiltrada = value;
            if (unidadeEpidemiologicaCombobox != null)
            {
                ObservableCollection<UnidadeEpidemiologicaComDetalhes> dados =
                    value == null
                        ? _cacheService.UEList
                        : new ObservableCollection<UnidadeEpidemiologicaComDetalhes>(
                            _cacheService.UEList.Where(p => p.propriedadeId == value.id));

                unidadeEpidemiologicaCombobox.ItemsSource = dados;
                unidadeEpidemiologicaCombobox.IsEnabled = dados.Any();

                // Seleciona automaticamente se há apenas um item
                if (dados.Count == 1)
                {
                    SelectedItem = dados[0];
                }
                else
                {
                    SelectedItem = null;
                }
            }

            NotifyChanged();
            OnPropertyChanged(nameof(TemMaisdeUma));
            Debug.WriteLine($"[UnidadeEpidemiologicaComboBox] Filtrado por Propriedade: {value?.nome}");
        }

        /// <summary>
        /// Seleciona automaticamente se há apenas um item.
        /// </summary>
        public void CheckJustOne()
        {
            if (UnidadeEpidemiologicaList.Count == 1 && unidadeEpidemiologicaCombobox != null)
            {
                SelectedItem = UnidadeEpidemiologicaList[0];
            }
            OnPropertyChanged(nameof(TemMaisdeUma));
        }

        #endregion

        #region Comandos e Eventos

        [RelayCommand(CanExecute = nameof(CanExecuteAddNewComboBox))]
        private async Task AddNewComboBoxAsync()
        {
            HapticHelper.VibrateClick();
            var ue = new UnidadeEpidemiologica { propriedadeId = _propriedadeFiltrada?.id };
            await NavigationUtils.ShowViewAsModalAsync<UnidadeEpidemiologicaView_Edit>(ue);
        }

        private bool CanExecuteAddNewComboBox()
            => Permissoes.UsuarioPermissoes?.unidadesEpidemiologicas.cadastrar ?? false;

        private void OnComboSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (unidadeEpidemiologicaCombobox != null && SelectedItem != unidadeEpidemiologicaCombobox.SelectedItem)
            {
                SelectedItem = (UnidadeEpidemiologicaComDetalhes?)unidadeEpidemiologicaCombobox.SelectedItem;
                Command?.Execute(null);
                
                Debug.WriteLine($"[UnidadeEpidemiologicaComboBox] SelectionChanged: {SelectedItem?.nome}");
            }
        }

        private void ClearSelected(object sender, EventArgs e)
        {
            SelectedItem = null;

            Debug.WriteLine("[UnidadeEpidemiologicaComboBox] Seleção limpa");
        }

        public async Task UnidadeEpidemiologicaAdicionada(int? unidadeID)
        {
            await AtualizaDados();
            SelectedItem = _cacheService.UEList.FirstOrDefault(u => u.id == unidadeID);

            Debug.WriteLine($"[UnidadeEpidemiologicaComboBox] Nova UE adicionada: {unidadeID}");
        }

        public void NotifyChanged()
        {
            OnPropertyChanged(nameof(EstaSelecionado));
            OnPropertyChanged(nameof(SelectedItem));
            OnPropertyChanged(nameof(TrailingViewWidthRequest));
            OnPropertyChanged(nameof(PrecisaMostrarApagar));
            AddNewComboBoxCommand.NotifyCanExecuteChanged();
        }

        protected override bool ComputeHasError()
        {
            bool temErro = IsObrigatorio && SelectedItem == null && IsAnyValidationActive && !IsReadOnly;
            Debug.WriteLine($"[UnidadeEpidemiologicaComboBox] Validação: Erro={temErro}");
            return temErro;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
        }

        #endregion

        #region BindableProperties

        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(
                nameof(SelectedItem),
                typeof(UnidadeEpidemiologicaComDetalhes),
                typeof(UnidadeEpidemiologicaComboBox),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnSelectedItemChanged);

        private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is UnidadeEpidemiologicaComboBox control)
            {
                if (control.unidadeEpidemiologicaCombobox != null)
                    control.unidadeEpidemiologicaCombobox.SelectedItem = newValue;

                control.NotifyChanged();
                control.OnPropertyChanged(nameof(ShowRequiredStar));
                control.ScheduleValidationRefresh();
            }
        }

        public UnidadeEpidemiologicaComDetalhes? SelectedItem
        {
            get => (UnidadeEpidemiologicaComDetalhes?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(
                nameof(Command),
                typeof(ICommand),
                typeof(UnidadeEpidemiologicaComboBox),
                default(ICommand));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(UnidadeEpidemiologicaComboBox),
                false,
                BindingMode.TwoWay,
                propertyChanged: ReadOnlyChanged);

        private static void ReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is UnidadeEpidemiologicaComboBox control)
            {
                control.OnPropertyChanged(nameof(IsReadOnly));
                control.OnPropertyChanged(nameof(NotIsReadOnly));
                control.OnPropertyChanged(nameof(PrecisaMostrarApagar));
                control.ScheduleValidationRefresh();

                Debug.WriteLine($"[UnidadeEpidemiologicaComboBox] IsReadOnly={newValue}");
            }
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool NotIsReadOnly => !IsReadOnly;

        public static readonly BindableProperty IsObrigatorioProperty =
            BindableProperty.Create(
                nameof(IsObrigatorio),
                typeof(bool),
                typeof(UnidadeEpidemiologicaComboBox),
                false,
                propertyChanged: (bindable, oldValue, newValue) => 
                {
                    if (bindable is UnidadeEpidemiologicaComboBox control)
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

        #endregion
    }
}
