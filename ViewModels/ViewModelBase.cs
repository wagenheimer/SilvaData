using System.Text;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// Base class for all view models
    /// - Implements INotifyPropertyChanged for WinRT
    /// - Implements some basic validation logic
    /// - Implements some IsBusy logic
    /// </summary>
    public partial class ViewModelBase : ObservableObject, IReadyOnly
    {
        /// <summary>
        /// Força notificação de alteração de todas as propriedades
        /// </summary>
        protected void RaiseAllPropertiesChanged()
        {
            // CommunityToolkit.Mvvm: string.Empty notifica todos os bindings
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Event for when IsBusy changes
        /// </summary>
        public event EventHandler IsBusyChanged;

        /// <summary>
        /// Event for when IsValid changes
        /// </summary>
        public event EventHandler IsValidChanged;

        readonly List<string> errors = new List<string>();
        bool isBusy = false;


        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        bool salvou = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ViewModelBase()
        {
            //Make sure validation is performed on startup
            Validate();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor("ReadOnly")]
        public bool isReadOnly;

        public bool ReadOnly => IsReadOnly;

        public bool NotBusy => !IsBusy;


        /// <summary>
        /// Returns true if the current state of the ViewModel is valid
        /// </summary>
        public bool IsValid
        {
            get { return errors.Count == 0; }
        }

        /// <summary>
        /// A list of errors if IsValid is false
        /// </summary>
        protected List<string> Errors
        {
            get { return errors; }
        }

        /// <summary>
        /// An aggregated error message
        /// </summary>
        public virtual string Error
        {
            get
            {
                return errors.Aggregate(new StringBuilder(), (b, s) => b.AppendLine(s)).ToString().Trim();
            }
        }

        /// <summary>
        /// Protected method for validating the ViewModel
        /// - Fires PropertyChanged for IsValid and Errors
        /// </summary>
        protected virtual void Validate()
        {
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(Errors));

            var method = IsValidChanged;
            if (method != null)
                method(this, EventArgs.Empty);
        }

        /// <summary>
        /// Other viewmodels should call this when overriding Validate, to validate each property
        /// </summary>
        /// <param name="validate">Func to determine if a value is valid</param>
        /// <param name="error">The error message to use if not valid</param>
        protected virtual void ValidateProperty(Func<bool> validate, string error)
        {
            if (validate())
            {
                if (!Errors.Contains(error))
                    Errors.Add(error);
            }
            else
            {
                Errors.Remove(error);
            }
        }

        /// <summary>
        /// Value indicating if a spinner should be shown
        /// </summary>
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                if (isBusy != value)
                {
                    isBusy = value;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanged("IsBusy");
                        OnIsBusyChanged();
                    });
                }
            }
        }

        /// <summary>
        /// Other viewmodels can override this if something should be done when busy
        /// </summary>
        protected virtual void OnIsBusyChanged()
        {
            OnPropertyChanged(nameof(NotBusy));

            var ev = IsBusyChanged;
            if (ev != null)
            {
                ev(this, EventArgs.Empty);
            }
        }



        [RelayCommand]
        public virtual async Task Voltar()
        {
            await NavigationUtils.PopModalAsync();
        }

        /// <summary>
        /// ✅ NOVO: Método virtual para cleanup de recursos
        /// Pode ser sobrescrito por ViewModels que precisam limpar estado
        /// </summary>
        public virtual void Cleanup()
        {
            IsBusy = false;
            IsRefreshing = false;
            Salvou = false;
        }

    }
}
