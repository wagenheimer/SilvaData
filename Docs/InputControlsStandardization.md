# Controles de Entrada Personalizados (ISIControls)

Documentação dos controles de input customizados do SilvaData.
Todos os arquivos ficam em `Pages/Controls/ISIControls/`.

## Padrão Visual (definido em 2026-04-13)

Todo campo de entrada segue estas regras:

| Propriedade | Valor |
|---|---|
| Contorno | `Stroke="{StaticResource PrimaryColor}"` (`#0888CD`) |
| Espessura normal | `StrokeThickness="1"` |
| Espessura erro | `StrokeThickness="2"` + `Stroke="Red"` |
| Altura | `HeightRequest="52"` no elemento `Border` |
| Cantos | `RoundRectangle CornerRadius="8"` |
| Fundo do Border | `Background="White"` |
| Inset horizontal interno | `Padding="12,0"` no `Border` |
| Fundo dos filhos | `Background/BackgroundColor="Transparent"` |
| Inset do título | `Padding="5,0"` no `Label` de título |

### Checklist para novos campos

- [ ] `Border` com `Stroke="{StaticResource PrimaryColor}"` e `StrokeThickness="1"`
- [ ] `Border` com `HeightRequest="52"` (exceto YesNoToggle)
- [ ] `Border` com `StrokeShape="RoundRectangle 8"` ou `<RoundRectangle CornerRadius="8"/>`
- [ ] `Border` com `Background="White"`
- [ ] `DataTrigger` no Border: `HasError=True` → `Stroke="Red"`, `StrokeThickness="2"`
- [ ] Controle filho com `Background/BackgroundColor="Transparent"`
- [ ] `SfComboBox` / `SfNumericEntry` com `ShowBorder="False"`
- [ ] Label de título com `TextColor="{StaticResource PrimaryColor}"`
- [ ] Asterisco `*` vermelho controlado por `IsRequired` / `IsObrigatorio`

---

## Controles Principais

### ISITextField
**Arquivo:** `Pages/Controls/ISIControls/ISITextField.xaml`  
**Herda de:** `ContentView`  
**Uso:** Campo de texto livre (string). Suporta modo senha com toggle de visibilidade.

**Propriedades bindáveis:**
| Propriedade | Tipo | Descrição |
|---|---|---|
| `Title` | string | Label acima do campo |
| `Text` | string | Valor (TwoWay) |
| `IsRequired` | bool | Exibe asterisco vermelho |
| `IsReadOnly` | bool | Desabilita edição |
| `HasError` | bool | Ativa borda vermelha |
| `IsPassword` | bool | Modo senha com toggle |
| `Placeholder` | string | Texto placeholder |

**Estrutura XAML:**
```
VerticalStackLayout [Padding="2", Spacing="4"]
  Grid (título + asterisco)
  Border [HeightRequest=52, Stroke=PrimaryColor, Padding="12,0", Background=White]
    DataTrigger: HasError → Stroke=Red
    RoundRectangle CornerRadius=8
    Grid [ColumnDefinitions="*,Auto"]
      Entry [BackgroundColor=Transparent, VerticalOptions=Fill]
      Label (ícone toggle senha — visível quando IsPassword=True)
```

---

### ISIDoubleValue
**Arquivo:** `Pages/Controls/ISIControls/ISIDoubleValue.xaml`  
**Herda de:** `ValidatableFieldBase`  
**Uso:** Campo decimal (double). Usa `SfNumericEntry` da Syncfusion.

**Propriedades bindáveis:**
| Propriedade | Tipo | Descrição |
|---|---|---|
| `Title` | string | Label acima do campo |
| `Value` | double? | Valor (TwoWay) |
| `IsObrigatorio` | bool | Asterisco vermelho |
| `IsReadOnly` | bool | Desabilita edição |
| `HasError` | bool | Ativa borda vermelha |
| `HelperText` | string | Texto auxiliar abaixo |

**Estrutura XAML:**
```
StackLayout [Padding="2", Spacing="4"]
  Grid (título + asterisco)
  Border [HeightRequest=52, Stroke=PrimaryColor, Padding="5,0", Background=White]
    DataTrigger: HasError → Stroke=Red
    RoundRectangle CornerRadius=8
    Grid [RowDefinitions="52"]
      SfNumericEntry [Background=Transparent, ShowBorder=False, AllowNull=True, ShowClearButton=False]
  Label (helper text)
```

> **Atenção:** O estilo global em `ISIStyles.xaml` força `Background="White"` em `SfNumericEntry`.
> O controle sobrescreve com `Background="Transparent"` inline — não remover.

---

### ISIComboBox
**Arquivo:** `Pages/Controls/ISIControls/ISIComboBox.xaml`  
**Herda de:** `ValidatableFieldBase`  
**Uso:** Seleção de item de lista cujos objetos têm propriedade `.nome`. Usa `SfComboBox` da Syncfusion.

**Propriedades bindáveis:**
| Propriedade | Tipo | Descrição |
|---|---|---|
| `Title` | string | Label acima do campo |
| `ItemsSource` | IEnumerable | Lista de itens |
| `SelectedItem` | object | Item selecionado (TwoWay) |
| `Text` | string | Texto digitado (TwoWay) |
| `IsRequired` | bool | Asterisco vermelho |
| `IsReadOnly` | bool | Desabilita edição |
| `HasError` | bool | Ativa borda vermelha |

**Estrutura XAML:**
```
VerticalStackLayout [Padding="2", Spacing="4"]
  Grid (título + asterisco)
  Border [HeightRequest=52, Stroke=PrimaryColor, Padding="0", Background=White]
    DataTrigger: HasError → Stroke=Red
    RoundRectangle CornerRadius=8
    SfComboBox [HeightRequest=52, ShowBorder=false, DisplayMemberPath="nome"]
```

> **Atenção:** `DisplayMemberPath="nome"` — diferente de `ComboAlternativas` que usa `"descricao"`.

---

### ISIDatePicker
**Arquivo:** `Pages/Controls/ISIControls/ISIDatePicker.xaml`  
**Herda de:** `ValidatableFieldBase`  
**Uso:** Seleção de data com botão calendário (ícone FontAwesome).

**Propriedades bindáveis:**
| Propriedade | Tipo | Descrição |
|---|---|---|
| `Hint` | string | Label acima do campo |
| `Data` | DateTime? | Data selecionada (TwoWay) |
| `IsObrigatorio` | bool | Asterisco vermelho |
| `IsReadOnly` | bool | Desabilita edição |
| `HasError` | bool | Ativa borda vermelha |
| `MinimumDate` | DateTime | Data mínima permitida |
| `MaximumDate` | DateTime | Data máxima permitida |

**Estrutura XAML:**
```
Grid [RowDefinitions="Auto,Auto", Padding="2"]
  Grid row=0 (hint + asterisco) [IsVisible=Hint not null/empty]
  Border row=1 [HeightRequest=52, Stroke=PrimaryColor, Background=White, Padding="0"]
    DataTrigger: HasError → Stroke=Red
    StrokeShape="RoundRectangle 8"
    Grid [ColumnDefinitions="*,44"]
      DatePicker [Background=Transparent, BackgroundColor=Transparent, HeightRequest=52, Format="dd/MM/yyyy"]
      Button (calendário — FontAwesomeLight, WidthRequest=44)
  Label (mensagem de erro — IsVisible=False por padrão)
```

---

### ISITimePicker
**Arquivo:** `Pages/Controls/ISIControls/ISITimePicker.xaml`  
**Herda de:** `ValidatableFieldBase`  
**Uso:** Seleção de horário com botão para limpar (ícone lixeira).

**Propriedades bindáveis:**
| Propriedade | Tipo | Descrição |
|---|---|---|
| `Hint` | string | Label acima do campo |
| `Hora` | TimeSpan? | Horário (TwoWay) |
| `IsObrigatorio` | bool | Asterisco vermelho |
| `IsReadOnly` | bool | Desabilita edição |
| `HasError` | bool | Ativa borda vermelha |
| `PrecisaMostrarApagar` | bool | Exibe botão lixeira |

**Estrutura XAML:**
```
StackLayout [Padding="2"]
  Grid (hint + asterisco) [IsVisible=Hint not null/empty]
  Border [HeightRequest=52, Stroke=PrimaryColor, Background=White, Padding="10,0"]
    DataTrigger: HasError → Stroke=Red
    StrokeShape="RoundRectangle 8"
    Grid [ColumnDefinitions="*,Auto"]
      TimePicker [BackgroundColor=Transparent, HeightRequest=52, Format="HH:mm"]
      Button (lixeira — IsVisible=PrecisaMostrarApagar)
```

---

### ISINumericUpDown
**Arquivo:** `Pages/Controls/ISIControls/ISINumericUpDown.xaml`  
**Herda de:** `ContentView`  
**Uso:** Controle primitivo de incremento/decremento. **Não usar diretamente em formulários** — encapsular em `ValorInteiroComBotoes` que adiciona Border, título e validação.

**Propriedades bindáveis:**
| Propriedade | Tipo | Descrição |
|---|---|---|
| `Value` | int | Valor atual (TwoWay) |
| `Minimum` | int | Valor mínimo |
| `Maximum` | int | Valor máximo |
| `IsReadOnly` | bool | Oculta botões +/- |
| `FontSize` | double | Tamanho da fonte |
| `TextColor` | Color | Cor do texto |
| `FontAttributes` | FontAttributes | Bold/Italic/None |

**Estrutura XAML:**
```
Grid [ColumnDefinitions="Auto,*,Auto"]
  Button (–) [HeightRequest=40, WidthRequest=40, IsVisible=IsNotReadOnly]
  Entry [BackgroundColor=Transparent, HorizontalTextAlignment=Center, Keyboard=Numeric]
  Button (+) [HeightRequest=40, WidthRequest=40, IsVisible=IsNotReadOnly]
```

---

## Controles de Parâmetros Dinâmicos (ParametrosComAlternativas/)

Usados nas telas de **Avaliação de Alternativas** e similares, onde os parâmetros vêm do servidor.
O `BindingContext` contém o objeto de parâmetro com propriedades `nome`, `required` (int 0/1/null), etc.

### ValorInteiroComBotoes
**Arquivo:** `Pages/Controls/ISIControls/ParametrosComAlternativas/ValorInteiroComBotoes.xaml`  
**Herda de:** `ValidatableFieldBase`  
**Uso:** Campo inteiro com botões +/- para parâmetros dinâmicos.

**Propriedades bindáveis:**
| Propriedade | Fonte | Descrição |
|---|---|---|
| `nome` | BindingContext | Título do campo |
| `required` | BindingContext | 0/1/null — asterisco |
| `Valor` | ValidatableFieldBase | Valor int (TwoWay) |
| `IsReadOnly` | ValidatableFieldBase | Desabilita +/- |
| `HasError` | ValidatableFieldBase | Borda vermelha |
| `HelperText` | ValidatableFieldBase | Texto auxiliar |
| `ShowHelperText` | ValidatableFieldBase | Visibilidade helper |
| `ParametroComAlternativas.valorMinimo/Maximo` | ValidatableFieldBase | Min/Max do updown |

**Estrutura XAML:**
```
StackLayout [Padding="2"]
  Grid (título + asterisco via DataTrigger em required)
  Border [HeightRequest=52, Stroke=PrimaryColor, Padding="5,0", Background=White]
    DataTrigger: HasError → Stroke=Red
    StrokeShape="RoundRectangle 8"
    ISINumericUpDown [FontSize=18/Desktop=24]
  Label (helper text)
```

---

### ValorDouble
**Arquivo:** `Pages/Controls/ISIControls/ParametrosComAlternativas/ValorDouble.xaml`  
**Herda de:** `ValidatableFieldBase`  
**Uso:** Campo decimal para parâmetros dinâmicos. Usa `Entry` nativo (não `SfNumericEntry`).

**Estrutura XAML:**
```
StackLayout [Padding="2"]
  Grid (título + asterisco via DataTrigger)
  Border [Stroke=PrimaryColor, Padding="5,0", Background=White]
    Grid [RowDefinitions="52"]
      Entry [Background=Transparent, Keyboard=Numeric, Placeholder="0.00"]
  Label (helper text)
```

> **Diferença do ISIDoubleValue:** usa `Entry` nativo com `Keyboard=Numeric`, não `SfNumericEntry`.
> Conversão via `StringToNullableDoubleConverter`.

---

### CampoTexto
**Arquivo:** `Pages/Controls/ISIControls/ParametrosComAlternativas/CampoTexto.xaml`  
**Herda de:** `ValidatableFieldBase`  
**Uso:** Wrapper fino que reutiliza `ISITextField` para parâmetros de texto.

**Estrutura XAML:**
```
ValidatableFieldBase
  ISITextField [Title=nome, Text=Texto, HasError, IsReadOnly, IsRequired]
```

---

### ComboAlternativas
**Arquivo:** `Pages/Controls/ISIControls/ParametrosComAlternativas/ComboAlternativas.xaml`  
**Herda de:** `ValidatableFieldBase`  
**Uso:** Combo para parâmetros dinâmicos. `DisplayMemberPath="descricao"` (≠ ISIComboBox que usa `"nome"`).

**Estrutura XAML:**
```
Grid [RowDefinitions="Auto,Auto"]
  Grid row=0 (título + asterisco via DataTrigger)
  Border row=1 [HeightRequest=52, Stroke=PrimaryColor, Padding="5,0"]
    TapGestureRecognizer → OpenComboBox (não remover!)
    SfComboBox [BackgroundColor=Transparent, DisplayMemberPath="descricao", HeightRequest=52, ShowBorder=False]
```

> **Atenção:** Tem `TapGestureRecognizer` na Border (`OpenComboBox`). Não remover ao editar o XAML.

---

### YesNoToggle
**Arquivo:** `Pages/Controls/ISIControls/ParametrosComAlternativas/YesNoToggle.xaml`  
**Herda de:** `ValidatableFieldBase`  
**Uso:** Toggle Sim/Não para parâmetros booleanos. Layout horizontal — não segue padrão de altura 52px.

**Estrutura XAML:**
```
Border [StrokeThickness=0 (sem borda visível por padrão), Padding="10,10"]
  DataTrigger: HasError → StrokeThickness=2, Stroke=Red
  Grid [ColumnDefinitions="*,60,60"]
    Grid (label + asterisco via DataTrigger)
    SfButton (Sim — ✓, HeightRequest=50, CornerRadius=40)
    SfButton (Não — ✗, HeightRequest=50, CornerRadius=40)
```

> **Não aplicar HeightRequest=52** — layout próprio horizontal.
> `StrokeThickness=0` por padrão; borda só aparece no estado de erro.
