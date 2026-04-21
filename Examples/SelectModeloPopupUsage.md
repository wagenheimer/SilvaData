# Exemplo de Uso de SelectModeloPopup

## Exemplo 1: Selecionando um item de uma lista genérica

```csharp
// Lista de itens para selecionar
var clientes = await Database.Instance.GetClientesAsync();

// Usando o popup diretamente
var clienteSelecionado = await SelectModeloPopup<Cliente>.ShowAsync(
    "Selecione um Cliente",
    clientes, 
    cliente => $"{cliente.Nome} ({cliente.CNPJ})"
);

if (clienteSelecionado != null)
{
    // Processa o cliente selecionado
    await DisplayAlert("Cliente Selecionado", 
        $"Você selecionou: {clienteSelecionado.Nome}", "OK");
}
```

## Exemplo 2: Usando as extensões para ISI Macro

```csharp
// Obter modelos do banco de dados
var modelos = await ModeloIsiMacro.PegaModelosIsiMacroComParametrosAsync();

// Usando a extensão de página
var modeloSelecionado = await this.ShowISIMacroSelectorAsync(
    "Selecione um Modelo", 
    modelos
);

if (modeloSelecionado != null)
{
    // Usar o modelo selecionado
    await ProcessarModeloAsync(modeloSelecionado);
}
```

## Exemplo 3: Criando um seletor personalizado

```csharp
// Classe de item para selecionar
public class OpcaoConfiguracao
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public bool Ativa { get; set; }
}

// Lista de configurações
var opcoes = new List<OpcaoConfiguracao>
{
    new() { Id = "config1", Nome = "Configuração A", Descricao = "Descrição da config A", Ativa = true },
    new() { Id = "config2", Nome = "Configuração B", Descricao = "Descrição da config B", Ativa = true },
    new() { Id = "config3", Nome = "Configuração C", Descricao = "Descrição da config C", Ativa = false }
};

// Filtra apenas opções ativas
var opcoesAtivas = opcoes.Where(o => o.Ativa).ToList();

// Usando a extensão para selecionar
var opcaoSelecionada = await this.ShowModeloSelectorAsync(
    "Selecione uma Configuração",
    opcoesAtivas,
    o => $"{o.Nome} - {o.Descricao}"
);

if (opcaoSelecionada != null)
{
    // Usar a opção selecionada
    AplicarConfiguracao(opcaoSelecionada.Id);
}
```