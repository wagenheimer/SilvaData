using System.Collections;
using System.Diagnostics;

using SelectionChangedEventArgs = Syncfusion.Maui.Inputs.SelectionChangedEventArgs;

namespace SilvaData.Controls
{
    /// <summary>
    /// Um controle ComboBox genérico que implementa <see cref="ICampoObrigatorio"/>.
    /// </summary>
    public partial class ISIComboBox : ValidatableFieldBase
    {
        #region Bindable Properties

        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(nameof(Text), typeof(string), typeof(ISIComboBox), string.Empty, BindingMode.TwoWay);

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(ISIComboBox), string.Empty);

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(ISIComboBox),
                false,
                propertyChanged: IsReadOnlyChanged);

        private static void IsReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ISIComboBox)bindable;
            var isReadOnly = (bool)newValue;

            control.isiComboBox.IsEnabled = !isReadOnly;
            control.OnPropertyChanged(nameof(IsReadOnly));

            Debug.WriteLine($"[ISIComboBox] IsReadOnly mudou para: {isReadOnly}");
            control.ScheduleValidationRefresh();
        }

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(ISIComboBox), null, propertyChanged: OnItemsSourceChanged);

        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(ISIComboBox), null, BindingMode.TwoWay, propertyChanged: OnSelectedItemChanged);

        public static readonly BindableProperty DisplayMemberPathProperty =
            BindableProperty.Create(nameof(DisplayMemberPath), typeof(string), typeof(ISIComboBox), "nome", propertyChanged: OnDisplayMemberPathChanged);

        public static readonly BindableProperty IsRequiredProperty =
            BindableProperty.Create(
                nameof(IsRequired), 
                typeof(bool), 
                typeof(ISIComboBox), 
                false,
                propertyChanged: (bindable, oldValue, newValue) => 
                {
                    if (bindable is ISIComboBox control)
                    {
                        control.OnPropertyChanged(nameof(ShowRequiredStar));
                    }
                });

        #endregion

        // Prevents re-entrancy/infinite loops when syncing selection both ways
        private bool _isSyncingSelection;

        #region Property Wrappers

        public IList? ItemsSource
        {
            get => (IList?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public bool IsRequired
        {
            get => (bool)GetValue(IsRequiredProperty);
            set => SetValue(IsRequiredProperty, value);
        }

        /// <summary>
        /// Define se o asterisco de campo obrigatório deve ser exibido.
        /// Visível apenas se for obrigatório E ainda não estiver preenchido.
        /// </summary>
        public bool ShowRequiredStar => IsRequired && SelectedItem == null;

        #endregion

        #region PropertyChanged Callbacks

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ISIComboBox)bindable;
            control.isiComboBox.ItemsSource = (IList?)newValue;

            // Ensure SelectedItem remains valid when the source changes or disappears
            if (newValue is not IList list || control.SelectedItem != null && !list.Contains(control.SelectedItem))
            {
                control.SelectedItem = null;
            }

            control.OnPropertyChanged(nameof(PrecisaMostrarApagar));
            control.ScheduleValidationRefresh();
        }

        private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ISIComboBox)bindable;

            control.OnPropertyChanged(nameof(ShowRequiredStar));

            if (Equals(control.isiComboBox.SelectedItem, newValue))
            {
                control.OnPropertyChanged(nameof(PrecisaMostrarApagar));
                return;
            }

            try
            {
                control._isSyncingSelection = true;
                control.isiComboBox.SelectedItem = newValue;
            }
            finally
            {
                control._isSyncingSelection = false;
            }

            control.OnPropertyChanged(nameof(PrecisaMostrarApagar));
            control.ScheduleValidationRefresh();
        }

        private static void OnDisplayMemberPathChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((ISIComboBox)bindable).isiComboBox.DisplayMemberPath = (string)newValue;
        }

        #endregion

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ISIComboBox"/>.
        /// </summary>
        public ISIComboBox()
        {
            InitializeComponent();
        }

        protected override void OnContextAttached()
        {
            HasError = false;
        }

        protected override void OnContextCleared()
        {
            HasError = false;
            SelectedItem = null;
            isiComboBox.SelectedItem = null;
            Text = string.Empty;
        }

        /// <summary>
        /// Chamado quando o *usuário* seleciona um item no SfComboBox *interno*.
        /// </summary>
        private void OnComboBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return;

            object? next = e.AddedItems?.Count > 0 ? e.AddedItems[0] : null;

            // If user cleared the selection, propagate null
            if (next == null && (e.RemovedItems?.Count ?? 0) > 0)
                next = null;

            if (!Equals(SelectedItem, next))
                SelectedItem = next;

            OnPropertyChanged(nameof(PrecisaMostrarApagar));
            ScheduleValidationRefresh();
        }

        protected override bool ComputeHasError()
        {
            return IsRequired && SelectedItem == null && IsAnyValidationActive && !IsReadOnly;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            var titleLabel = this.FindByName<Label>("labelTitle");
            var border = this.FindByName<Border>("isiComboBoxBorder");

            ValidationVisualHelper.ApplyTitleColor(titleLabel, hasError);

            if (border != null)
            {
                border.Stroke = hasError ? Colors.Red : ValidationVisualHelper.GetPrimaryColor();
                border.StrokeThickness = hasError ? 2 : 1;
            }
        }

        public bool PrecisaMostrarApagar => isiComboBox.SelectedIndex >= 0 || SelectedItem != null;

        private void ClearSelected(object sender, EventArgs e)
        {
            SelectedItem = null;
        }

        private void ShowPopUp(object sender, EventArgs e)
        {
            isiComboBox.Focus();
        }
    }
}
