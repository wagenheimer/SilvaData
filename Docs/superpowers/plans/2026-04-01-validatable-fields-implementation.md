# Validatable Fields Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Centralizar o ciclo de validacao visual dos campos obrigatorios em uma base reutilizavel e migrar os controles atuais para eliminar repeticao e perda de estado no scroll.

**Architecture:** Criar uma `ValidatableFieldBase` enxuta para concentrar estado de validacao, mensagens globais e agendamento de refresh, mantendo a regra de "campo vazio" e a aplicacao visual como responsabilidade dos controles filhos. Um helper visual pequeno sera usado apenas para cor primaria e atualizacao de titulos.

**Tech Stack:** .NET MAUI, ContentView, BindableProperty, CommunityToolkit.Mvvm.Messaging, Syncfusion MAUI controls

---

### Task 1: Criar infraestrutura compartilhada de validacao

**Files:**
- Create: `Pages/Controls/Common/ValidatableFieldBase.cs`
- Create: `Pages/Controls/Common/ValidationVisualHelper.cs`
- Test: validacao manual inicial via migracao do primeiro controle

- [ ] **Step 1: Criar a base abstrata `ValidatableFieldBase`**

```csharp
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Utilities;

namespace SilvaData.Controls;

public abstract class ValidatableFieldBase : ContentView, ICampoObrigatorio
{
    private bool _isValidationActive;

    public static readonly BindableProperty HasErrorProperty =
        BindableProperty.Create(
            nameof(HasError),
            typeof(bool),
            typeof(ValidatableFieldBase),
            false);

    public bool HasError
    {
        get => (bool)GetValue(HasErrorProperty);
        protected set => SetValue(HasErrorProperty, value);
    }

    protected bool IsValidationActive => _isValidationActive;
    protected bool IsAnyValidationActive => ISIUtils.IsValidationActiveGlobal || _isValidationActive;

    protected ValidatableFieldBase()
    {
        WeakReferenceMessenger.Default.Register<HighlightRequiredFieldsMessage>(this, (_, __) =>
        {
            _isValidationActive = true;
            ScheduleValidationRefresh();
        });

        WeakReferenceMessenger.Default.Register<ClearValidationErrorsMessage>(this, (r, _) =>
        {
            var control = (ValidatableFieldBase)r;
            control._isValidationActive = false;
            control.HasError = false;
            control.ApplyValidationVisualState(false);
            control.ScheduleValidationRefresh();
        });
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext == null)
        {
            HasError = false;
            ApplyValidationVisualState(false);
            OnContextCleared();
            return;
        }

        OnContextAttached();

        if (ISIUtils.IsValidationActiveGlobal)
        {
            _isValidationActive = true;
        }

        ScheduleValidationRefresh();
    }

    public bool PreenchidoCorretamente()
    {
        bool hasError = ComputeHasError();
        HasError = hasError;
        ApplyValidationVisualState(hasError);
        return !hasError;
    }

    protected void ScheduleValidationRefresh()
    {
        Action refresh = () => PreenchidoCorretamente();

        if (Dispatcher != null)
        {
            Dispatcher.Dispatch(refresh);
            return;
        }

        if (Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.Dispatch(refresh);
            return;
        }

        refresh();
    }

    protected virtual void OnContextAttached() { }
    protected virtual void OnContextCleared() { }
    protected virtual void ApplyValidationVisualState(bool hasError) { }
    protected abstract bool ComputeHasError();
}
```

- [ ] **Step 2: Criar helper visual compartilhado**

```csharp
namespace SilvaData.Controls;

public static class ValidationVisualHelper
{
    public static Color GetPrimaryColor()
    {
        if (Application.Current?.Resources != null &&
            Application.Current.Resources.TryGetValue("PrimaryColor", out var color) &&
            color is Color resourceColor)
        {
            return resourceColor;
        }

        return Colors.Blue;
    }

    public static void ApplyTitleColor(Label? label, bool hasError)
    {
        if (label == null)
        {
            return;
        }

        label.TextColor = hasError ? Colors.Red : GetPrimaryColor();
    }
}
```

- [ ] **Step 3: Verificar que os novos arquivos compilam semanticamente com o namespace atual**

Run: revisar namespaces e referencias para `ICampoObrigatorio`, `HighlightRequiredFieldsMessage`, `ClearValidationErrorsMessage` e `ISIUtils`
Expected: sem conflitos de namespace e sem duplicacao de `HasErrorProperty`

- [ ] **Step 4: Commit**

```bash
git add Pages/Controls/Common/ValidatableFieldBase.cs Pages/Controls/Common/ValidationVisualHelper.cs
git commit -m "refactor: add shared validatable field infrastructure"
```

### Task 2: Migrar o primeiro grupo de controles base

**Files:**
- Modify: `Pages/Controls/ISIControls/ParametrosComAlternativas/CampoTexto.xaml.cs`
- Modify: `Pages/Controls/ISIControls/ISIComboBox.xaml.cs`
- Modify: `Pages/Controls/ISIControls/ParametrosComAlternativas/ValorInteiroComBotoes.xaml.cs`
- Test: validacao manual nesses tres controles

- [ ] **Step 1: Migrar `CampoTexto` para herdar de `ValidatableFieldBase`**

```csharp
public partial class CampoTexto : ValidatableFieldBase
{
    protected override void OnContextCleared()
    {
        ParametroComAlternativas = null;
        Texto = null;
        SyncInnerFieldErrorState(false);
    }

    protected override void OnContextAttached()
    {
        if (BindingContext is ParametroComAlternativas param)
        {
            ParametroComAlternativas = param;
            OnParametroChanged();
        }
        else
        {
            ParametroComAlternativas = null;
            Texto = null;
            SyncInnerFieldErrorState(false);
        }
    }

    protected override bool ComputeHasError()
    {
        return ParametroComAlternativas?.required == 1
            && string.IsNullOrWhiteSpace(Texto)
            && IsAnyValidationActive
            && NotIsReadOnly;
    }

    protected override void ApplyValidationVisualState(bool hasError)
    {
        SyncInnerFieldErrorState(hasError);
    }
}
```

- [ ] **Step 2: Migrar `ISIComboBox` para usar a base**

```csharp
public partial class ISIComboBox : ValidatableFieldBase
{
    protected override void OnContextCleared()
    {
        HasError = false;
        ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), false);
    }

    protected override void OnContextAttached()
    {
        ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), false);
    }

    protected override bool ComputeHasError()
    {
        return IsRequired && SelectedItem == null && IsAnyValidationActive && !IsReadOnly;
    }

    protected override void ApplyValidationVisualState(bool hasError)
    {
        ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
    }
}
```

- [ ] **Step 3: Migrar `ValorInteiroComBotoes` para usar a base**

```csharp
public partial class ValorInteiroComBotoes : ValidatableFieldBase
{
    protected override void OnContextCleared()
    {
        ParametroComAlternativas = null;
        Valor = null;
        OnPropertyChanged(nameof(ValorTexto));
        OnPropertyChanged(nameof(HelperText));
        OnPropertyChanged(nameof(ShowHelperText));
    }

    protected override void OnContextAttached()
    {
        if (BindingContext is not ParametroComAlternativas param)
        {
            ParametroComAlternativas = null;
            Valor = null;
            return;
        }

        ParametroComAlternativas = param;

        if (ParametroComAlternativas.ValorInt.HasValue)
            Valor = ParametroComAlternativas.ValorInt;
        else if (int.TryParse(ParametroComAlternativas.valorPadrao, out int valorPadraoInt))
            Valor = valorPadraoInt;

        OnPropertyChanged(nameof(ValorTexto));
        OnPropertyChanged(nameof(HelperText));
        OnPropertyChanged(nameof(ShowHelperText));
    }

    protected override bool ComputeHasError()
    {
        if (ParametroComAlternativas != null)
            ParametroComAlternativas.ValorInt = Valor;

        return ParametroComAlternativas?.required == 1
            && !Valor.HasValue
            && IsAnyValidationActive
            && !IsReadOnly;
    }

    protected override void ApplyValidationVisualState(bool hasError)
    {
        ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
    }
}
```

- [ ] **Step 4: Validar manualmente o trio base**

Run:
- abrir tela com campos obrigatorios
- acionar validacao
- fazer scroll para fora e para dentro
- verificar persistencia de titulo e borda

Expected:
- `CampoTexto` continua vermelho apos scroll
- `ISIComboBox` continua vermelho apos scroll
- `ValorInteiroComBotoes` nao apresenta intermitencia

- [ ] **Step 5: Commit**

```bash
git add Pages/Controls/ISIControls/ParametrosComAlternativas/CampoTexto.xaml.cs Pages/Controls/ISIControls/ISIComboBox.xaml.cs Pages/Controls/ISIControls/ParametrosComAlternativas/ValorInteiroComBotoes.xaml.cs
git commit -m "refactor: migrate core required field controls to shared validation base"
```

### Task 3: Migrar os demais controles de parametros

**Files:**
- Modify: `Pages/Controls/ISIControls/ParametrosComAlternativas/ValorDouble.xaml.cs`
- Modify: `Pages/Controls/ISIControls/ISIDoubleValue.xaml.cs`
- Modify: `Pages/Controls/ISIControls/ISIDatePicker.xaml.cs`
- Modify: `Pages/Controls/ISIControls/ISITimePicker.xaml.cs`
- Modify: `Pages/Controls/ISIControls/ParametrosComAlternativas/YesNoToggle.xaml.cs`
- Modify: `Pages/Controls/ISIControls/ParametrosComAlternativas/ComboAlternativas.xaml.cs`

- [ ] **Step 1: Migrar `ValorDouble`**

```csharp
protected override bool ComputeHasError()
{
    if (ParametroComAlternativas != null)
    {
        ParametroComAlternativas.ValorDouble = Valor;
    }

    return ParametroComAlternativas?.required == 1
        && !Valor.HasValue
        && IsAnyValidationActive
        && !IsReadOnly;
}

protected override void ApplyValidationVisualState(bool hasError)
{
    ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
}
```

- [ ] **Step 2: Migrar `ISIDoubleValue`, `ISIDatePicker` e `ISITimePicker`**

```csharp
protected override bool ComputeHasError()
{
    return IsObrigatorio
        && CampoEstaVazio()
        && IsAnyValidationActive
        && !IsReadOnly;
}

protected override void ApplyValidationVisualState(bool hasError)
{
    ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
    ErrorLabel.IsVisible = hasError; // apenas no DatePicker
}
```

- [ ] **Step 3: Migrar `YesNoToggle`**

```csharp
protected override bool ComputeHasError()
{
    bool campoEstaVazio = ParametroComAlternativas?.ValorSimNao == null;
    return ParametroComAlternativas?.required == 1
        && campoEstaVazio
        && IsAnyValidationActive;
}

protected override void ApplyValidationVisualState(bool hasError)
{
    OnPropertyChanged(nameof(TextColor));
}
```

- [ ] **Step 4: Migrar `ComboAlternativas` sem quebrar a restauracao assincrona**

```csharp
protected override bool ComputeHasError()
{
    bool selecionado = ParametroComAlternativas?.SelectedIndex >= 0;
    bool obrigatorio = ParametroComAlternativas?.required == 1;
    return obrigatorio && !selecionado && NotIsReadOnly && IsAnyValidationActive;
}

protected override void ApplyValidationVisualState(bool hasError)
{
    ValidationVisualHelper.ApplyTitleColor(labelTitle, hasError);
}
```

- [ ] **Step 5: Validar manualmente os controles migrados**

Run:
- acionar validacao com todos vazios
- scroll para cima e para baixo
- preencher e limpar novamente

Expected:
- todos mantem vermelho apos scroll
- `ComboAlternativas` nao perde restauracao de itens
- `YesNoToggle` atualiza corretamente o texto e a cor

- [ ] **Step 6: Commit**

```bash
git add Pages/Controls/ISIControls/ParametrosComAlternativas/ValorDouble.xaml.cs Pages/Controls/ISIControls/ISIDoubleValue.xaml.cs Pages/Controls/ISIControls/ISIDatePicker.xaml.cs Pages/Controls/ISIControls/ISITimePicker.xaml.cs Pages/Controls/ISIControls/ParametrosComAlternativas/YesNoToggle.xaml.cs Pages/Controls/ISIControls/ParametrosComAlternativas/ComboAlternativas.xaml.cs
git commit -m "refactor: migrate remaining parameter controls to shared validation base"
```

### Task 4: Migrar os combos customizados

**Files:**
- Modify: `Pages/Controls/CustomControls/ComboBox/RegionalComboBox.xaml.cs`
- Modify: `Pages/Controls/CustomControls/ComboBox/PropriedadeComboBox.xaml.cs`
- Modify: `Pages/Controls/CustomControls/ComboBox/UnidadeEpidemiologicaComboBox.xaml.cs`

- [ ] **Step 1: Migrar `RegionalComboBox`**

```csharp
public partial class RegionalComboBox : ValidatableFieldBase
{
    protected override bool ComputeHasError()
    {
        return IsObrigatorio && SelectedItem == null && IsAnyValidationActive && !IsReadOnly;
    }

    protected override void ApplyValidationVisualState(bool hasError)
    {
        ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
    }
}
```

- [ ] **Step 2: Migrar `PropriedadeComboBox`**

```csharp
protected override bool ComputeHasError()
{
    return IsObrigatorio && SelectedItem == null && IsAnyValidationActive && !IsReadOnly;
}

protected override void ApplyValidationVisualState(bool hasError)
{
    ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
}
```

- [ ] **Step 3: Migrar `UnidadeEpidemiologicaComboBox`**

```csharp
protected override bool ComputeHasError()
{
    return IsObrigatorio && SelectedItem == null && IsAnyValidationActive && !IsReadOnly;
}

protected override void ApplyValidationVisualState(bool hasError)
{
    ValidationVisualHelper.ApplyTitleColor(this.FindByName<Label>("labelTitle"), hasError);
}
```

- [ ] **Step 4: Validar manualmente os custom combos**

Run:
- abrir tela que use esses combos
- validar campos obrigatorios
- fazer scroll, troca de pagina e retorno

Expected:
- borda e titulo permanecem consistentes
- selecao continua sincronizada com ViewModel

- [ ] **Step 5: Commit**

```bash
git add Pages/Controls/CustomControls/ComboBox/RegionalComboBox.xaml.cs Pages/Controls/CustomControls/ComboBox/PropriedadeComboBox.xaml.cs Pages/Controls/CustomControls/ComboBox/UnidadeEpidemiologicaComboBox.xaml.cs
git commit -m "refactor: migrate custom combo controls to shared validation base"
```

### Task 5: Limpeza, verificacao e adocao do padrao

**Files:**
- Modify: `Docs/superpowers/specs/2026-04-01-validatable-fields-design.md` (se arquitetura final divergir)
- Modify: quaisquer arquivos migrados com codigo morto removido

- [ ] **Step 1: Remover codigo duplicado que ficar obsoleto**

```csharp
// Remover de controles migrados:
// - campos _isValidationActive duplicados
// - registros locais repetidos de messenger
// - ScheduleValidationRefresh duplicado
// - blocos repetidos de ApplyTitleColor
```

- [ ] **Step 2: Rodar build do projeto**

Run: `dotnet build SilvaData.csproj -nologo`
Expected: build concluido sem erros de compilacao

- [ ] **Step 3: Executar checklist manual de regressao**

Run:
- validar todos os campos obrigatorios principais
- scroll para fora e para dentro
- preencher campos e confirmar limpeza do erro
- disparar `ClearValidationErrorsMessage`

Expected:
- nenhum campo permanece com estado divergente entre titulo e borda
- nenhum campo "comeca vermelho" sem validacao ativa
- nenhum campo perde o vermelho depois do scroll

- [ ] **Step 4: Atualizar o spec se a implementacao final exigir ajuste de arquitetura**

```markdown
- registrar no spec qualquer override extra necessario
- registrar excecoes reais encontradas em `ComboAlternativas` ou combos customizados
```

- [ ] **Step 5: Commit**

```bash
git add Pages/Controls Docs/superpowers/specs/2026-04-01-validatable-fields-design.md Docs/superpowers/plans/2026-04-01-validatable-fields-implementation.md
git commit -m "refactor: standardize required field validation behavior"
```
