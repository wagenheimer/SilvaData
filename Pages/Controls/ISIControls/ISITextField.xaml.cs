namespace SilvaData.Controls
{
    using System.Diagnostics;
    using CommunityToolkit.Mvvm.Messaging;
    using SilvaData.Utilities;
    using SilvaData.FontAwesome;

    /// <summary>
    /// Um controle de entrada de texto customizado que inclui um título (Label)
    /// e implementa a interface <see cref="ICampoObrigatorio"/> para validação.
    /// </summary>
    public partial class ISITextField : ContentView, ICampoObrigatorio
    {
        #region Bindable Properties

        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(
                nameof(Text), 
                typeof(string), 
                typeof(ISITextField), 
                string.Empty, 
                BindingMode.TwoWay,
                propertyChanged: (bindable, oldValue, newValue) => 
                {
                    if (bindable is ISITextField control)
                    {
                        control.OnPropertyChanged(nameof(ShowRequiredStar));
                        control.PreenchidoCorretamente();
                    }
                });

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(ISITextField), string.Empty);

        public static readonly BindableProperty PlaceholderProperty =
            BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(ISITextField), string.Empty);

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(ISITextField), false);

        public static readonly BindableProperty IsPasswordProperty =
            BindableProperty.Create(
                nameof(IsPassword),
                typeof(bool),
                typeof(ISITextField),
                false,
                propertyChanged: OnIsPasswordChanged);

        public static readonly BindableProperty IsRequiredProperty =
            BindableProperty.Create(
                nameof(IsRequired), 
                typeof(bool), 
                typeof(ISITextField), 
                false,
                propertyChanged: (bindable, oldValue, newValue) => 
                {
                    if (bindable is ISITextField control)
                    {
                        control.OnPropertyChanged(nameof(ShowRequiredStar));
                    }
                });

        public static readonly BindableProperty HasErrorProperty =
            BindableProperty.Create(
                nameof(HasError),
                typeof(bool),
                typeof(ISITextField),
                false,
                propertyChanged: OnHasErrorChanged);

        /// <summary>
        /// Obtém ou define o valor do texto da entrada.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Obtém ou define o texto exibido no Label acima da entrada.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Obtém ou define o texto de placeholder da entrada.
        /// </summary>
        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        /// <summary>
        /// Obtém ou define um valor que indica se o campo é somente leitura.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Obtém ou define um valor que indica se a entrada deve mascarar o texto (para senhas).
        /// </summary>
        public bool IsPassword
        {
            get => (bool)GetValue(IsPasswordProperty);
            set => SetValue(IsPasswordProperty, value);
        }

        /// <summary>
        /// Obtém ou define um valor que indica se o campo é obrigatório.
        /// </summary>
        public bool IsRequired
        {
            get => (bool)GetValue(IsRequiredProperty);
            set => SetValue(IsRequiredProperty, value);
        }

        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            set => SetValue(HasErrorProperty, value);
        }

        /// <summary>
        /// Define se o asterisco de campo obrigatório deve ser exibido.
        /// Visível apenas se for obrigatório E ainda não estiver preenchido.
        /// </summary>
        public bool ShowRequiredStar => IsRequired && string.IsNullOrWhiteSpace(Text);

        #endregion

        #region Private Properties

        private bool _isPasswordVisible = false;
        private bool _isValidationActive = false;

        private Page? GetOwningPage()
        {
            Element? current = this;
            while (current != null)
            {
                if (current is Page page)
                {
                    return page;
                }

                current = current.Parent;
            }

            return null;
        }

        private bool IsMessageForThisControl(Page? targetPage)
        {
            return targetPage == null || ReferenceEquals(GetOwningPage(), targetPage);
        }

        private bool IsGlobalValidationActiveForThisControl()
        {
            return ISIUtils.IsValidationActiveGlobal
                && ISIUtils.ValidationTargetPage != null
                && ReferenceEquals(GetOwningPage(), ISIUtils.ValidationTargetPage);
        }

        /// <summary>
        /// Controla se a senha está visível (false = oculta com bolinhas, true = texto visível).
        /// </summary>
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                _isPasswordVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PasswordIconText));
                OnPropertyChanged(nameof(ShouldMaskPassword));
            }
        }

        /// <summary>
        /// Retorna true se o texto deve ser mascarado (IsPassword=true E IsPasswordVisible=false).
        /// </summary>
        public bool ShouldMaskPassword => IsPassword && !IsPasswordVisible;

        /// <summary>
        /// Retorna o ícone apropriado para o botão de toggle de senha.
        /// Eye = senha oculta (clique para mostrar)
        /// EyeSlash = senha visível (clique para ocultar)
        /// </summary>
        public string PasswordIconText => IsPasswordVisible
            ? FontAwesomeIcons.EyeSlash  // Senha visível -> mostrar ícone de ocultar
            : FontAwesomeIcons.Eye;       // Senha oculta -> mostrar ícone de revelar

        #endregion

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ISITextField"/>.
        /// </summary>
        public ISITextField()
        {
            try
            {
                InitializeComponent();
                WeakReferenceMessenger.Default.Register<HighlightRequiredFieldsMessage>(this, (recipient, message) => 
                {
                    var control = (ISITextField)recipient;
                    if (!control.IsMessageForThisControl(message.TargetPage))
                    {
                        return;
                    }

                    control._isValidationActive = true;
                    control.PreenchidoCorretamente();
                });
                WeakReferenceMessenger.Default.Register<ClearValidationErrorsMessage>(this, (r, message) => 
                {
                    var c = (ISITextField)r;
                    if (!c.IsMessageForThisControl(message.TargetPage))
                    {
                        return;
                    }

                    c._isValidationActive = false;
                    c.HasError = false;
                    c.PreenchidoCorretamente(); // Para resetar a cor do cabeçalho
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] ISITextField: {ex.Message}");
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
            
            // SÓ reseta quando o item REALMENTE sai da tela (BindingContext null)
            if (BindingContext == null)
            {
                _isValidationActive = false;
                HasError = false;
            }
            
            var titleLabel = this.FindByName<Label>("labelTitle");
            if (titleLabel != null)
            {
                Color primaryColor = ISIUtils.GetResourceColor("PrimaryColor", Colors.Blue);
                titleLabel.TextColor = primaryColor;
            }
            
            // Sincroniza flag local com estado global e agenda revalidação após bindings se atualizarem
            if (BindingContext != null && IsGlobalValidationActiveForThisControl())
            {
                _isValidationActive = true;
                Dispatcher.Dispatch(() => PreenchidoCorretamente());
            }
        }

        /// <summary>
        /// Chamado quando a propriedade IsPassword é alterada.
        /// </summary>
        private static void OnIsPasswordChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ISITextField control)
            {
                // Quando IsPassword é definido, reseta o estado para oculto
                if ((bool)newValue)
                {
                    control.IsPasswordVisible = false;
                }
                control.OnPropertyChanged(nameof(ShouldMaskPassword));
            }
        }

        private static void OnHasErrorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ISITextField control && newValue is bool hasError)
            {
                control.ApplyValidationVisualState(hasError);
            }
        }

        /// <summary>
        /// Handler do evento de clique no botão de toggle de senha.
        /// </summary>
        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        /// <summary>
        /// Implementação da interface <see cref="ICampoObrigatorio"/>.
        /// Verifica se o campo <see cref="Text"/> está preenchido e atualiza a cor do Label.
        /// </summary>
        /// <returns><c>true</c> se o campo estiver preenchido; caso contrário, <c>false</c>.</returns>
        public bool PreenchidoCorretamente()
        {
            // ✅ Sincronização Extra: Se Text estiver vazio mas o Entry tiver texto, força a atualização.
            var entry = this.FindByName<Entry>("entryField");
            if (string.IsNullOrWhiteSpace(Text) && entry != null && !string.IsNullOrWhiteSpace(entry.Text))
            {
                Text = entry.Text;
                Debug.WriteLine($"[VALIDATION] Sincronização rápida em ISITextField: Text atualizado para '{Text}'");
            }

            bool campoEstaVazio = string.IsNullOrWhiteSpace(Text);
            bool temErro = IsRequired && campoEstaVazio && (IsGlobalValidationActiveForThisControl() || _isValidationActive) && !IsReadOnly;
            
            HasError = temErro;

            if (temErro)
            {
                Debug.WriteLine($"[VALIDATION ERROR] Campo '{Title}' (ISITextField) é obrigatório mas está vazio.");
            }

            return !campoEstaVazio;
        }

        /// <summary>
        /// Remove o registro de mensagens quando o controle é destruído.
        /// </summary>
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler == null)
            {
                WeakReferenceMessenger.Default.Unregister<HighlightRequiredFieldsMessage>(this);
                WeakReferenceMessenger.Default.Unregister<ClearValidationErrorsMessage>(this);
            }
        }

        private void ApplyValidationVisualState(bool hasError)
        {
            var titleLabel = this.FindByName<Label>("labelTitle");
            var borderField = this.FindByName<Border>("borderField");
            if (titleLabel == null)
            {
                return;
            }

            Color primaryColor = Colors.Blue;
            if (Application.Current?.Resources != null &&
                Application.Current.Resources.TryGetValue("PrimaryColor", out var color) &&
                color is Color resourceColor)
            {
                primaryColor = resourceColor;
            }

            titleLabel.TextColor = hasError ? Colors.Red : primaryColor;

            if (borderField != null)
            {
                borderField.Stroke = hasError ? Colors.Red : primaryColor;
                borderField.StrokeThickness = hasError ? 2 : 1;
            }
        }
    }
}
