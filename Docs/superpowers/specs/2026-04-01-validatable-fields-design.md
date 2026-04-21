# Validatable Fields Design

**Goal:** Centralizar o comportamento de validacao visual de campos obrigatorios para evitar repeticao, divergencia entre controles e perda de estado durante scroll com reciclacgem de celulas.

**Contexto atual**

Hoje varios controles repetem a mesma estrutura:
- flag `_isValidationActive`
- propriedade `HasError`
- registro de `HighlightRequiredFieldsMessage`
- registro de `ClearValidationErrorsMessage`
- revalidacao no `OnBindingContextChanged`
- atualizacao manual de titulo, borda ou estado visual

Essa repeticao esta causando dois tipos de problema:
- correcoes campo a campo, com comportamento diferente entre controles
- bugs intermitentes quando o item recicla no scroll e parte do estado visual se perde

## Abordagens avaliadas

### 1. Interface + helper estatico

Cada controle continuaria herdando direto de `ContentView` e usaria um helper compartilhado para revalidacao e aplicacao visual.

**Vantagens**
- migracao leve
- pouco impacto em heranca atual

**Desvantagens**
- nao centraliza de verdade o ciclo de vida
- facilita nova divergencia entre controles
- exige disciplina manual em todos os campos

### 2. Base class unica para tudo

Criar uma classe base com estado, ciclo de vida e regras visuais completas.

**Vantagens**
- elimina bastante repeticao
- padroniza comportamento de forma forte

**Desvantagens**
- risco de acoplamento excessivo com detalhes de UI
- controles com estrutura muito diferente podem forcar overrides demais

### 3. Base class enxuta + helper visual

Criar uma base para o ciclo de validacao e deixar a aplicacao visual especifica em override ou helper pequeno.

**Vantagens**
- centraliza o comportamento que hoje quebra no scroll
- reduz repeticao sem acoplar a base aos detalhes de cada controle
- permite encaixar tanto `ISIControls` quanto `CustomControls/ComboBox`

**Desvantagens**
- exige uma migracao inicial coordenada

**Recomendacao:** abordagem 3.

## Arquitetura proposta

Criar `ValidatableFieldBase : ContentView, ICampoObrigatorio`.

Responsabilidades da base:
- manter `_isValidationActive`
- expor `HasError`
- registrar e remover mensagens de highlight/clear
- implementar `ScheduleValidationRefresh()`
- implementar o fluxo padrao de `PreenchidoCorretamente()`
- padronizar `OnBindingContextChanged()` para nao perder o estado durante reciclacgem

Contrato dos controles filhos:
- `protected abstract bool ComputeHasError()`
- `protected virtual void ApplyValidationVisualState(bool hasError)`
- `protected virtual void OnContextAttached()`
- `protected virtual void OnContextCleared()`

## Fluxo padrao da base

### HighlightRequiredFieldsMessage

Quando o controle recebe highlight:
- `_isValidationActive = true`
- agenda `ScheduleValidationRefresh()`

### ClearValidationErrorsMessage

Quando o controle recebe clear:
- `_isValidationActive = false`
- `HasError = false`
- aplica visual limpo
- agenda nova revalidacao apenas para sincronizar UI com bindings tardios

### OnBindingContextChanged

Quando `BindingContext == null`:
- nao apagar o historico de validacao por simples reciclacgem visual
- limpar apenas o que for estado contextual do controle
- aplicar visual limpo temporario se necessario

Quando `BindingContext != null`:
- chamar `OnContextAttached()`
- se a validacao global ou local estiver ativa, agendar revalidacao no `Dispatcher`
- caso contrario, aplicar somente o estado visual neutro

### PreenchidoCorretamente

Fluxo:
1. calcular `hasError` via `ComputeHasError()`
2. atualizar `HasError`
3. chamar `ApplyValidationVisualState(hasError)`
4. retornar `!hasError`

## Helper visual

Criar `ValidationVisualHelper` com responsabilidade minima:
- `GetPrimaryColor()`
- `ApplyTitleColor(Label? label, bool hasError)`

O helper nao deve conhecer regras de negocio nem estado de validacao.

## Estrategia de migracao

### Fase 1: extrair infraestrutura comum

Criar:
- `Pages/Controls/Common/ValidatableFieldBase.cs`
- `Pages/Controls/Common/ValidationVisualHelper.cs`

### Fase 2: migrar controles dos parametros

Prioridade:
- `CampoTexto`
- `ISIComboBox`
- `ValorInteiroComBotoes`
- `ValorDouble`
- `ISIDatePicker`
- `ISITimePicker`
- `ISIDoubleValue`
- `YesNoToggle`
- `ComboAlternativas`

### Fase 3: migrar custom combos

Migrar:
- `RegionalComboBox`
- `PropriedadeComboBox`
- `UnidadeEpidemiologicaComboBox`

Esses controles devem herdar o ciclo de validacao comum, mas manter sua logica propria de carga, cache e sincronizacao de selecao.

## Exemplo de responsabilidade por controle

### CampoTexto
- `ComputeHasError()`: texto vazio, obrigatorio, nao readonly, validacao ativa
- `ApplyValidationVisualState()`: sincronizar `isiTextField.HasError`

### ISIComboBox
- `ComputeHasError()`: `SelectedItem == null`
- `ApplyValidationVisualState()`: pintar titulo

### ValorInteiroComBotoes
- `ComputeHasError()`: `!Valor.HasValue`
- `ApplyValidationVisualState()`: pintar titulo

### YesNoToggle
- `ComputeHasError()`: `ValorSimNao == null`
- `ApplyValidationVisualState()`: disparar `OnPropertyChanged(nameof(TextColor))`

### ComboAlternativas
- manter restauracao de estado e carregamento assincrono
- delegar apenas o ciclo de validacao comum para a base

## Regras de design

- a base nao deve conhecer `Label`, `Border`, `Entry`, `SfComboBox` ou `Syncfusion`
- a base deve cuidar de estado e timing, nao de layout
- controles filhos continuam donos da regra de "campo vazio"
- a migracao deve preservar o comportamento atual de cada controle, mudando apenas a infraestrutura repetida

## Riscos e mitigacoes

### Risco: base class virar classe grande demais

**Mitigacao:** manter a base restrita ao ciclo de validacao e ao agendamento de refresh.

### Risco: controles muito diferentes exigirem muitos overrides

**Mitigacao:** deixar `ApplyValidationVisualState()` opcional e mover apenas o necessario para helper pequeno.

### Risco: regressao em controles com carregamento assincrono

**Mitigacao:** migrar `ComboAlternativas` e `CustomControls/ComboBox` por ultimo, mantendo seus fluxos de restauracao intactos.

## Critérios de sucesso

- nenhum controle validavel precisa reimplementar o ciclo basico de validacao
- o estado vermelho permanece apos scroll em todos os controles migrados
- `HighlightRequiredFieldsMessage` e `ClearValidationErrorsMessage` passam a ter comportamento uniforme
- novos controles validaveis passam a ser criados por heranca da base, sem copiar blocos antigos
