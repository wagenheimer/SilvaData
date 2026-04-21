using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData_MAUI.Models;
using SilvaData_MAUI.Utilities;

using System.Collections.ObjectModel;
using System.Linq;

namespace SilvaData_MAUI.Controls
{
    /// <summary>
    /// Página para visualizar e gerenciar a lista de Proprietários.
    /// MIGRADO: Usa CacheService ao invés de DadosStatic.
    /// </summary>
    public partial class ProprietarioView : ContentPageWithLocalization
    {
        private readonly CacheService _cacheService;

        public string TextoPesquisa { get; set; }

        public ObservableCollection<Proprietario> ListaProprietarios { get; private set; }

        /// <summary>
        /// MIGRADO: Construtor agora recebe CacheService via DI
        /// </summary>
        public ProprietarioView(CacheService cacheService)
        {
            InitializeComponent();

            _cacheService = cacheService;
            ListaProprietarios = new ObservableCollection<Proprietario>();

            BindingContext = this;
        }

        #region Ciclo de Vida da Página

        /// <summary>
        /// Chamado quando a página está prestes a se tornar visível.
        /// Carrega os dados e registra os receptores de mensagens.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // MIGRADO: Usa CacheService ao invés de DadosStatic
            ListaProprietarios = new ObservableCollection<Proprietario>(_cacheService.ProprietarioList);

            // Notifica a UI que a coleção mudou
            OnPropertyChanged(nameof(ListaProprietarios));

            // Aplica o filtro
            RefreshData();

            // Registra as mensagens para atualizações em tempo real
            WeakReferenceMessenger.Default.Register<ProprietarioAdicionadoMessage>(this, (recipient, message) =>
            {
                ListaProprietarios.Insert(0, message.Proprietario);
            });

            WeakReferenceMessenger.Default.Register<ProprietarioSalvoMessage>(this, (recipient, message) =>
            {
                var proprietarioAlterado = ListaProprietarios.FirstOrDefault(p => p.id == message.Proprietario.id);
                if (proprietarioAlterado != null)
                {
                    int index = ListaProprietarios.IndexOf(proprietarioAlterado);
                    if (index != -1)
                        ListaProprietarios[index] = message.Proprietario;
                }
            });
        }

        /// <summary>
        /// Chamado quando a página não está mais visível.
        /// Remove o registro dos receptores de mensagens.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            WeakReferenceMessenger.Default.Unregister<ProprietarioAdicionadoMessage>(this);
            WeakReferenceMessenger.Default.Unregister<ProprietarioSalvoMessage>(this);
        }

        #endregion

        #region Comandos

        /// <summary>
        /// Comando para navegar para a tela de edição de um proprietário.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeEditar))]
        private async Task Edit(Proprietario proprietario)
        {
            await Editar(proprietario);
        }

        /// <summary>
        /// Comando para navegar para a tela de adição de um novo proprietário.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeAdicionar))]
        private async Task AddNew()
        {
            await NavigationUtils.ShowViewAsModalAsync<ProprietarioView_Edit>();
        }

        /// <summary>
        /// Comando para fechar a página modal atual.
        /// </summary>
        [RelayCommand]
        public async Task Voltar()
        {
            await NavigationUtils.PopModalAsync();
        }

        #endregion

        #region Permissões

        /// <summary>
        /// Obtém um valor que indica se o usuário pode editar proprietários.
        /// </summary>
        public bool PodeEditar => Permissoes.UsuarioPermissoes?.proprietarios.atualizar ?? false;

        /// <summary>
        /// Obtém um valor que indica se o usuário pode adicionar novos proprietários.
        /// </summary>
        public bool PodeAdicionar => Permissoes.UsuarioPermissoes?.proprietarios.cadastrar ?? false;

        #endregion

        #region Lógica de Filtro e Dados

        /// <summary>
        /// Atualiza o filtro da SfListView.
        /// </summary>
        public void RefreshData()
        {
            if (listaProprietarios.DataSource != null)
            {
                listaProprietarios.DataSource.Filter = filterData;
                listaProprietarios.DataSource.RefreshFilter();
            }
        }

        /// <summary>
        /// Método de predicado de filtro para a SfListView.
        /// </summary>
        private bool filterData(object obj)
        {
            if (obj is not Proprietario proprietario) return false;

            var displayThis = true;

            if (!string.IsNullOrEmpty(TextoPesquisa))
            {
                var searchBarText = LocalizationManager.RemoveDiacritics(TextoPesquisa.ToUpper());

                if (proprietario.nome == null || !LocalizationManager.RemoveDiacritics(proprietario.nome.ToUpper()).Contains(searchBarText))
                    displayThis = false;
            }
            return displayThis;
        }

        /// <summary>
        /// Manipulador de evento para alteração de texto na barra de pesquisa.
        /// </summary>
        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextoPesquisa = e.NewTextValue;
            RefreshData();
        }

        /// <summary>
        /// Navega para a página de edição de um proprietário específico.
        /// </summary>
        public async Task Editar(Proprietario proprietario)
        {
            await NavigationUtils.ShowViewAsModalAsync<ProprietarioView_Edit>(proprietario);
        }

        #endregion
    }
}
