using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Models;
using SilvaData.Utilities;
using System.Linq;

namespace SilvaData.Controls
{
    /// <summary>
    /// Página para visualizar e gerenciar a lista de Propriedades.
    /// MIGRADO: Usa CacheService ao invés de DadosStatic.
    /// </summary>
    public partial class PropriedadeView : ContentPageWithLocalization
    {
        private readonly CacheService _cacheService;

        public string TextoPesquisa { get; set; }

        /// <summary>
        /// A Regional selecionada para filtrar a lista (pode ser null).
        /// </summary>
        public Regional RegionalSelecionada { get; set; }

        public ObservableCollection<Propriedade> ListaPropriedades { get; private set; }

        /// <summary>
        /// MIGRADO: Construtor agora recebe CacheService via DI
        /// </summary>
        public PropriedadeView(CacheService cacheService)
        {
            InitializeComponent();

            _cacheService = cacheService;
            ListaPropriedades = new ObservableCollection<Propriedade>();

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
            ListaPropriedades = new ObservableCollection<Propriedade>(_cacheService.PropriedadeList);

            // Notifica a UI (SfListView) que a coleção mudou
            OnPropertyChanged(nameof(ListaPropriedades));

            // Aplica o filtro
            RefreshData();

            // Registra as mensagens para atualizações em tempo real
            WeakReferenceMessenger.Default.Register<PropriedadeAdicionadaMessage>(this, (recipient, message) =>
            {
                ListaPropriedades.Insert(0, message.Propriedade);
            });

            WeakReferenceMessenger.Default.Register<PropriedadeSalvaMessage>(this, (recipient, message) =>
            {
                var propriedadeAlterada = ListaPropriedades.FirstOrDefault(p => p.id == message.Propriedade.id);
                if (propriedadeAlterada != null)
                {
                    var index = ListaPropriedades.IndexOf(propriedadeAlterada);
                    if (index >= 0)
                        ListaPropriedades[index] = message.Propriedade;
                }
            });

            // ADICIONADO: Registra refresh do cache
            WeakReferenceMessenger.Default.Register<RefreshCacheMessage>(this, async (r, m) =>
            {
                if (m.Type == CacheType.Propriedades || m.Type == CacheType.All)
                {
                    ListaPropriedades = new ObservableCollection<Propriedade>(_cacheService.PropriedadeList);
                    OnPropertyChanged(nameof(ListaPropriedades));
                    RefreshData();
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

            WeakReferenceMessenger.Default.Unregister<PropriedadeAdicionadaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<PropriedadeSalvaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<RefreshCacheMessage>(this);
        }

        #endregion

        #region Comandos

        /// <summary>
        /// Comando para navegar para a tela de edição de uma propriedade.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeEditar))]
        private async Task Edit(Propriedade propriedade) => await Editar(propriedade);

        /// <summary>
        /// Comando para navegar para a tela de adição de uma nova propriedade.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeAdicionar))]
        private async Task AddNew()
        {
            await NavigationUtils.ShowViewAsModalAsync<PropriedadeView_Edit>();
        }

        /// <summary>
        /// Comando para navegar para a tela de Unidades Epidemiológicas.
        /// </summary>
        [RelayCommand]
        private async Task ShowUnidade()
        {
            await NavigationUtils.ShowViewAsModalAsync<UnidadeEpidemiologicaView>();
        }

        /// <summary>
        /// Comando para fechar a página modal atual.
        /// </summary>
        [RelayCommand]
        public async Task Voltar() => await NavigationUtils.PopModalAsync();

        /// <summary>
        /// Comando para forçar a atualização dos filtros da lista.
        /// </summary>
        [RelayCommand]
        public void AtualizaFiltros() => RefreshData();

        #endregion

        #region Permissões

        /// <summary>
        /// Obtém um valor que indica se o usuário pode editar propriedades.
        /// </summary>
        public bool PodeEditar => Permissoes.UsuarioPermissoes?.propriedades.atualizar ?? false;

        /// <summary>
        /// Obtém um valor que indica se o usuário pode adicionar propriedades.
        /// </summary>
        public bool PodeAdicionar => Permissoes.UsuarioPermissoes?.propriedades.cadastrar ?? false;

        #endregion

        #region Lógica de Filtro e Dados

        /// <summary>
        /// Atualiza o filtro da SfListView.
        /// </summary>
        public void RefreshData()
        {
            if (listaPropriedades.DataSource != null)
            {
                listaPropriedades.DataSource.Filter = filterData;
                listaPropriedades.DataSource.RefreshFilter();
            }
        }

        /// <summary>
        /// Método de predicado de filtro para a SfListView.
        /// </summary>
        private bool filterData(object obj)
        {
            if (obj is not Propriedade propriedade) return false;

            var displayThis = true;

            // Filtro por texto
            if (!string.IsNullOrEmpty(TextoPesquisa))
            {
                var searchBarText = LocalizationManager.RemoveDiacritics(TextoPesquisa.ToUpper());
                if (propriedade.nome == null || !LocalizationManager.RemoveDiacritics(propriedade.nome.ToUpper()).Contains(searchBarText))
                    displayThis = false;
            }

            // Filtro por Regional
            if (RegionalSelecionada != null && propriedade.regionalId != RegionalSelecionada.id)
                displayThis = false;

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
        /// Navega para a página de edição de uma propriedade específica.
        /// </summary>
        public async Task Editar(Propriedade propriedade)
        {
            await NavigationUtils.ShowViewAsModalAsync<PropriedadeView_Edit>(propriedade);
        }

        #endregion
    }
}
