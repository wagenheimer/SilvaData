# Exemplo de Uso de NavigationUtils

## Exemplo 1: Exibindo um popup simples

```csharp
// Criar um popup
var photoPopup = new PhotoPopup("caminho/da/imagem.jpg", "Título da Foto");

// Exibir o popup usando NavigationUtils
await NavigationUtils.ShowPopupAsync(photoPopup);
```

## Exemplo 2: Exibindo um popup com retorno tipado

```csharp
// Criar um popup que retorna resultado
var yesNoPopup = new PopUpYesNo("Confirmação", "Deseja continuar?");

// Exibir o popup e receber o resultado tipado (bool)
bool resposta = await NavigationUtils.ShowPopupAsync<bool>(yesNoPopup);

if (resposta)
{
    // Usuário escolheu "Sim"
}
else
{
    // Usuário escolheu "Não" ou fechou o popup
}
```

## Exemplo 3: Exibindo um popup ancorado a um elemento

```csharp
// Criar um popup
var modeloPopup = new SelectModeloPopup<Cliente>("Selecione um cliente", clientes, c => c.Nome);

// Exibir ancorado a um botão
await NavigationUtils.ShowPopupAsync(modeloPopup, meuBotao);

// O popup será exibido próximo ao botão em vez de centralizado na tela
```

## Exemplo 4: Exibindo um popup com resultado complexo

```csharp
// Criar um popup que retorna um objeto complexo
var fecharLotePopup = new PopUpFecharLote("Fechar Lote", "Informe os dados para fechamento");

// Exibir o popup e receber o resultado tipado
var resultado = await NavigationUtils.ShowPopupAsync<PopUpFecharLote.LoteFechamentoInfo>(fecharLotePopup);

if (resultado?.Confirmado == true)
{
    // Usuário confirmou com os dados:
    // resultado.DataFechamento
    // resultado.Observacoes
}
```