using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;

namespace SilvaData.Controls
{
    /// <summary>
    /// Página para visualização e gerenciamento de Unidades Epidemiológicas (UE).
    /// MIGRADO: Usa CacheService ao invés de DadosStatic.
    /// </summary>
    public partial class UnidadeEpidemiologicaView : ContentPageWithLocalization
    {
        private readonly CacheService _cacheService;

        public string TextoPesquisa { get; set; }

        public ObservableCollection<UnidadeEpidemiologica> ListaUE { get; private set; }

        /// <summary>
        /// MIGRADO: Construtor agora recebe CacheService via DI
        /// </summary>
        public UnidadeEpidemiologicaView(CacheService cacheService)
        {
            InitializeComponent();

            _cacheService = cacheService;
            ListaUE = new ObservableCollection<UnidadeEpidemiologica>();

            BindingContext = this;

            this.Loaded += OnPageLoaded;
            this.Unloaded += OnPageUnloaded;
        }

        #region Ciclo de Vida (Loaded/Unloaded/Appearing)

        /// <summary>
        /// Chamado quando a página é carregada. Registra mensagens persistentes.
        /// </summary>
        private void OnPageLoaded(object? sender, EventArgs e)
        {
            // Mensagens para esta lista
            WeakReferenceMessenger.Default.Register<UnidadeEpidemiologicaView, UEAdicionadaMessage>(this, OnUEAdicionadaMessage);
            WeakReferenceMessenger.Default.Register<UnidadeEpidemiologicaView, UESalvaMessage>(this, OnUESalvaMessage);

            // Mensagens de dependências (que podem ser abertas por modais)
            WeakReferenceMessenger.Default.Register<PropriedadeAdicionadaMessage>(this, (r, m) => RefreshDataOnMainThread());
            WeakReferenceMessenger.Default.Register<RegionalAdicionadaMessage>(this, (r, m) => RefreshDataOnMainThread());
        }

        /// <summary>
        /// Chamado quando a página é descarregada (destruída).
        /// Limpa os registros de mensagens.
        /// </summary>
        private void OnPageUnloaded(object? sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.Unregister<UEAdicionadaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<UESalvaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<PropriedadeAdicionadaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<RegionalAdicionadaMessage>(this);
        }

        /// <summary>
        /// Chamado quando a página está prestes a se tornar visível.
        /// Recarrega os dados da lista.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // MIGRADO: Usa CacheService ao invés de DadosStatic
            ListaUE = new ObservableCollection<UnidadeEpidemiologica>(_cacheService.UEList);
            OnPropertyChanged(nameof(ListaUE));
            RefreshData();
        }

        /// <summary>
        /// Substitui o comportamento do botão "Voltar" do dispositivo.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
            _ = NavigationUtils.PopModalAsync();
            return true;
        }

        #endregion

        #region Handlers de Mensagens

        /// <summary>
        /// Manipulador para a mensagem <see cref="UEAdicionadaMessage"/>.
        /// </summary>
        private static void OnUEAdicionadaMessage(UnidadeEpidemiologicaView recipient, UEAdicionadaMessage message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                recipient.ListaUE.Insert(0, message.UnidadeEpidemiologica);
            });
        }

        /// <summary>
        /// Manipulador para a mensagem <see cref="UESalvaMessage"/>.
        /// </summary>
        private static void OnUESalvaMessage(UnidadeEpidemiologicaView recipient, UESalvaMessage message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var ueAlterada = recipient.ListaUE.FirstOrDefault(p => p.id == message.UnidadeEpidemiologica.id);
                if (ueAlterada is not null)
                {
                    int index = recipient.ListaUE.IndexOf(ueAlterada);
                    if (index != -1)
                        recipient.ListaUE[index] = message.UnidadeEpidemiologica;
                }
            });
        }

        /// <summary>
        /// Atualiza os dados da lista na thread principal.
        /// Chamado por mensagens de Propriedade ou Regional.
        /// </summary>
        private void RefreshDataOnMainThread()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // MIGRADO: Usa CacheService ao invés de DadosStatic
                ListaUE = new ObservableCollection<UnidadeEpidemiologica>(_cacheService.UEList);
                OnPropertyChanged(nameof(ListaUE));
                RefreshData();
            });
        }

        #endregion

        #region Comandos

        [RelayCommand(CanExecute = nameof(PodeEditar))]
        private async Task Edit(UnidadeEpidemiologica ue)
        {
            await Editar(ue);
        }

        [RelayCommand(CanExecute = nameof(PodeAdicionar))]
        private async Task AddNew()
        {
            await NavigationUtils.ShowViewAsModalAsync<UnidadeEpidemiologicaView_Edit>();
        }

        [RelayCommand]
        private async Task ShowLote(UnidadeEpidemiologica ue)
        {
            var loteViewModel = ServiceHelper.GetRequiredService<LoteViewModel>();

            if (loteViewModel != null)
            {
                await loteViewModel.LimparFiltros();
                var ueComDetalhes = loteViewModel.UEList.FirstOrDefault(u => u.id == ue.id);

                loteViewModel.SelectedFiltroUE = ueComDetalhes;
                await loteViewModel.CarregaLotes();

                WeakReferenceMessenger.Default.Send(new ShowLotesMessage());
            }

            await Voltar();
        }

        [RelayCommand]
        public async Task Voltar()
        {
            await NavigationUtils.PopModalAsync();
        }

        [RelayCommand]
        public void AtualizaFiltros()
        {
            RefreshData();
        }

        #endregion

        #region Permissões

        public bool PodeEditar => Permissoes.UsuarioPermissoes?.regionais.atualizar ?? false;
        public bool PodeAdicionar => Permissoes.UsuarioPermissoes?.regionais.cadastrar ?? false;

        #endregion

        #region Lógica de Filtro e Dados

        /// <summary>
        /// Aplica o filtro atual à fonte de dados da SfListView.
        /// </summary>
        public void RefreshData()
        {
            if (listaUE.DataSource != null)
            {
                listaUE.DataSource.Filter = filterData;
                listaUE.DataSource.RefreshFilter();
            }
        }

        /// <summary>
        /// Método de predicado de filtro para a SfListView.
        /// </summary>
        private bool filterData(object obj)
        {
            if (obj is not UnidadeEpidemiologica ue) return false;

            var displayThis = true;

            if (!string.IsNullOrEmpty(TextoPesquisa))
            {
                var searchBarText = LocalizationManager.RemoveDiacritics(TextoPesquisa.ToUpper());

                if (ue.nome == null || !LocalizationManager.RemoveDiacritics(ue.nome.ToUpper()).Contains(searchBarText))
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
        /// Navega para a página de edição de uma UE específica.
        /// </summary>
        public async Task Editar(UnidadeEpidemiologica ue)
        {
            await NavigationUtils.ShowViewAsModalAsync<UnidadeEpidemiologicaView_Edit>(ue);
        }

        #endregion
    }

}
