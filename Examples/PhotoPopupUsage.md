## Como usar o novo PhotoPopup em .NET MAUI

### Opção 1: Usando as extensões (recomendado)
```csharp
// Em qualquer handler de evento ou método de comando
private async Task MostrarFoto(string caminhoImagem, string titulo)
{
    // A partir de qualquer página
    await this.ShowPhotoPopupAsync(caminhoImagem, titulo);
    
    // OU usando o método estático (quando não tiver acesso direto à página)
    await PopupExtensions.ShowPhotoPopupAsync(caminhoImagem, titulo);
}
```

### Opção 2: Criando o popup diretamente
```csharp
private async Task MostrarFoto(string caminhoImagem, string titulo)
{
    var popup = new PhotoPopup(caminhoImagem, titulo);
    await Shell.Current.CurrentPage.ShowPopupAsync(popup);
}
```

### Diferenças da versão anterior
1. Não use mais `PopupNavigation.Instance.PushAsync()` - use `page.ShowPopupAsync()` no MAUI
2. O namespace e hierarquia de classes é diferente:
   - Antes: `ISIInstitute.Views.PhotoPopup` herdando de `PopupPage`
   - Agora: `SilvaData.Pages.PopUps.PhotoPopup` herdando de `Popup`
3. Para fechar um popup:
   - Antes: `await PopupNavigation.Instance.PopAsync()`
   - Agora: `popup.Close()`

### Observações sobre a migração
- O pacote Rg.Plugins.Popup pode ser removido depois de migrar todos os popups
- A estrutura de UI manteve-se praticamente a mesma, só as classes base que mudaram
- As propriedades do popup (como `CanBeDismissedByTappingOutsideOfPopup`) são definidas diretamente no XAML
