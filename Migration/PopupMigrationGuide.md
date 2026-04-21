# Migração dos Popups para .NET MAUI

## Resumo da Migração

Todos os popups da aplicação foram migrados do Xamarin Forms (usando Rg.Plugins.Popup) para os popups nativos do .NET MAUI (usando CommunityToolkit.Maui.Views.Popup). Esta migração melhora a performance e compatibilidade com o .NET 10.

## Principais Alterações

1. **Base Class**: 
   - Antes: `Rg.Plugins.Popup.Pages.PopupPage`
   - Agora: `CommunityToolkit.Maui.Views.Popup`

2. **Namespaces**:
   - Antes: `xmlns:rg="clr-namespace:Rg.Plugins.Popup.Pages;assembly=Rg.Plugins.Popup"`
   - Agora: `xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"`

3. **Navegação**:
   - Antes: `await PopupNavigation.Instance.PushAsync(popup)`
   - Agora: `await page.ShowPopupAsync(popup)`

4. **Fechamento**:
   - Antes: `await PopupNavigation.Instance.PopAsync()`
   - Agora: `popup.Close(result)` (pode retornar um resultado)

## Popups Migrados

1. **PhotoPopup** - Exibe uma imagem com título
2. **PopUpYesNo** - Diálogo de confirmação com botões Sim/Não
3. **PopUpOK** - Diálogo informativo com botão OK
4. **PopUpNPS** - Formulário de avaliação Net Promoter Score
5. **PopUpFecharLote** - Formulário para fechamento de lote com data e observações
6. **PopUpUsuario** - Formulário de login de usuário
7. **PopUpPrivacy** - Exibe texto da política de privacidade com opções aceitar/recusar
8. **SelectModeloPopup** - Seleção de itens de uma lista com pesquisa

## Como Usar

Todas as funcionalidades foram encapsuladas em extensões na classe `PopupExtensions`:

```csharp
// Exemplo de uso com popup de foto
await this.ShowPhotoPopupAsync("caminho/da/imagem.jpg", "Título da Imagem");

// Exemplo de diálogo de confirmação
if (await this.ShowYesNoPopupAsync("Confirmação", "Deseja realmente excluir?"))
{
    // Usuário clicou em Sim
}

// Exemplo de seleção de modelo
var modelo = await this.ShowSelectModeloPopupAsync(
    "Selecione um Cliente", 
    listaClientes, 
    cliente => cliente.Nome
);

if (modelo != null)
{
    // Usuário selecionou um modelo
}
```

## Observações

- A antiga dependência `Rg.Plugins.Popup` pode ser removida após a migração completa.
- Os novos popups retornam resultados tipados, em vez de depender de eventos.
- Os métodos estáticos de extensão facilitam o uso em qualquer parte da aplicação.