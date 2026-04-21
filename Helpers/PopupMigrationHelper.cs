using System.Windows.Input;
using CommunityToolkit.Maui.Views;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// Como migrar os demais popups para MAUI
    /// </summary>
    public class PopupMigrationHelper
    {
        /// <summary>
        /// Lista de alterações necessárias para migrar popups do Xamarin para MAUI
        /// </summary>
        public static IEnumerable<string> MigrationSteps()
        {
            return new[]
            {
                "1. Alterar a herança de PopupPage para Popup (CommunityToolkit.Maui.Views)",
                "2. Alterar o namespace de ISIInstitute.Views.PopUps para SilvaData.Pages.PopUps",
                "3. Remover o atributo [XamlCompilation(XamlCompilationOptions.Compile)]",
                "4. Alterar os métodos de navegação:",
                "   - Navigation.PushPopupAsync() -> page.ShowPopupAsync()",
                "   - Navigation.PopPopupAsync() -> popup.Close(result)",
                "5. Substituir OnDisappearing pelo evento Closed",
                "6. Atualizar o XAML:",
                "   - xmlns:pages=\"http://rotorgames.com\" -> xmlns:toolkit=\"http://schemas.microsoft.com/dotnet/2022/maui/toolkit\"",
                "   - pages:PopupPage -> toolkit:Popup",
                "   - Atualizar os controles para equivalentes MAUI",
                "7. Adicionar métodos de extensão no PopupExtensions.cs para uso conveniente"
            };
        }
        
        /// <summary>
        /// Exemplos de como usar os popups migrados
        /// </summary>
        public static IEnumerable<(string Title, string Code)> UsageExamples()
        {
            return new[]
            {
                (
                    "PopUpOK",
                    @"// Exemplo 1: Usando o método estático
await PopUpOK.ShowAsync(""Título"", ""Mensagem"");

// Exemplo 2: Usando a extensão
await this.ShowOKPopupAsync(""Título"", ""Mensagem"");

// Exemplo 3: Usando a instância diretamente
var popup = new PopUpOK(""Título"", ""Mensagem"");
await Shell.Current.CurrentPage.ShowPopupAsync(popup);"
                ),
                (
                    "PopUpYesNo",
                    @"// Retorna true se Sim, false se Não
bool resposta = await this.ShowYesNoPopupAsync(""Confirmação"", ""Deseja continuar?"");
if (resposta)
{
    // Usuário escolheu Sim
}"
                ),
                (
                    "SelectModeloPopup",
                    @"// T é o tipo de objeto a ser selecionado
var modelo = await this.ShowSelectModeloPopupAsync<Cliente>(
    ""Selecione um cliente"", 
    listaClientes, 
    cliente => cliente.Nome
);

if (modelo != null)
{
    // Usuário selecionou um modelo
}"
                )
            };
        }
    }
}
