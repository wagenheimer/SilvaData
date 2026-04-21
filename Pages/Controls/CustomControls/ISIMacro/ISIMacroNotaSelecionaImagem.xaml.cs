using SilvaData.Models;
using SilvaData.ViewModels;

using System.Diagnostics;

namespace SilvaData.Controls
{

    public partial class ISIMacroNotaSelecionaImagem : ContentPage
    {
        private readonly ISIMacroNotaSelecionaImagemViewModel _viewModel;
        private string? _nome;
        private ParametroComAlternativas? _parametro;

        /// <summary>
        /// ✅ Construtor que recebe nome e parametro do NavigationUtils
        /// </summary>
        public ISIMacroNotaSelecionaImagem(string nome, ParametroComAlternativas parametro)
        {
            _nome = nome;
            _parametro = parametro;

            _viewModel = ServiceHelper.GetRequiredService<ISIMacroNotaSelecionaImagemViewModel>();

            // ✅ iOS/Android fix: configurar os dados ANTES do InitializeComponent/BindingContext
            // para que o SfImageEditor receba o Source correto já na primeira renderização.
            _viewModel.SetInitialState(nome, parametro);

            // ★ Diagnóstico: Verifica existência do arquivo
            var path = _viewModel.Alternativa?.urlImagemLocal;
            bool exists = !string.IsNullOrEmpty(path) && File.Exists(path);
            Debug.WriteLine($"[ISIMacroFoto] ★ Verificando Imagem: {path ?? "NULL"}");
            Debug.WriteLine($"[ISIMacroFoto] ★ Existe no disco? {exists}");

            InitializeComponent();

            BindingContext = _viewModel;

            // ✅ Fix: SfImageEditor não recarrega de forma confiável via binding MAUI quando
            // a Source muda (iOS e Android). Atualização explícita via code-behind garante
            // que o controle carregue e navegue corretamente entre alternativas.
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Debug.WriteLine($"[ISIMacroNotaSelecionaImagem] Construtor: {nome}");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Força reload explícito na primeira exibição (cobre edge case iOS/Android)
            _ = ReloadImageEditorAsync();
        }

        /// <summary>
        /// Recarrega a imagem no SfImageEditor via code-behind com o padrão null→source
        /// documentado pela Syncfusion para garantir reload confiável em ambas as plataformas.
        /// </summary>
        private async Task ReloadImageEditorAsync()
        {
            var path = _viewModel.Alternativa?.urlImagemLocal;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.WriteLine($"[ISIMacroFoto] ⚠️ ReloadImage: arquivo não existe: {path ?? "NULL"}");
                sfImageEditor.Source = null;
                return;
            }

            Debug.WriteLine($"[ISIMacroFoto] 🔄 ReloadImage: {path}");

            // Padrão null → source força o SfImageEditor a limpar o estado interno
            // antes de carregar a nova imagem, evitando cache stale.
            sfImageEditor.Source = null;
            await Task.Delay(30);

            // MemoryStream: stream re-legível (SfImageEditor pode tentar ler múltiplas vezes).
            // Padrão documentado pela Syncfusion para carregamento confiável via stream.
            var bytes = await Task.Run(() => File.ReadAllBytes(path));
            sfImageEditor.Source = ImageSource.FromStream(() => new MemoryStream(bytes));

            Debug.WriteLine($"[ISIMacroFoto] ✅ ReloadImage concluído");
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ISIMacroNotaSelecionaImagemViewModel.AlternativaImageSource))
                return;

            MainThread.BeginInvokeOnMainThread(() => _ = ReloadImageEditorAsync());
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel?.Cleanup();
            Debug.WriteLine("[ISIMacroNotaSelecionaImagem] Cleanup executado");
        }

        protected override bool OnBackButtonPressed()
        {
            try
            {
                _ = NavigationUtils.PopModalAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ISIMacroNotaSelecionaImagem] Erro: {ex.Message}");
                return base.OnBackButtonPressed();
            }
        }
    }
}
