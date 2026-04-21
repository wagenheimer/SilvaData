# Migração de SfNumericTextBox para SfNumericEntry

## Guia de Migração

De acordo com a documentação da Syncfusion, ao migrar do `SfNumericTextBox` do Xamarin.Forms para o `SfNumericEntry` do .NET MAUI, as seguintes alterações de propriedades devem ser feitas:

| Xamarin SfNumericTextBox | .NET MAUI SfNumericEntry | Descrição |
|--------------------------|--------------------------|-----------|
| BorderColor | Stroke | Define a cor da borda |
| ClearButtonVisibility | ShowClearButton | Controla a visibilidade do botão de limpar |
| FormatString | CustomFormat | Define o formato da exibição do valor |
| IsReadOnly | IsEditable | Controla se o usuário pode editar o valor |
| Watermark | Placeholder | Texto exibido quando não há valor |
| WatermarkColor | PlaceholderColor | Cor do texto do placeholder |
| MaximumNumberDecimalDigits | MaximumDecimalDigits | Número máximo de casas decimais |

## Exemplo de Migração

### Código Xamarin.Forms:
```xml
<numerictextbox:SfNumericTextBox
    BorderColor="White"
    FormatString="0.00"
    MaximumNumberDecimalDigits="2"
    Value="{Binding Amount}" />
```

### Código MAUI:
```xml
<syncfusion:SfNumericEntry
    Stroke="White"
    CustomFormat="0.00"
    MaximumDecimalDigits="2"
    Value="{Binding Amount}" />
```

## Recursos em breve disponíveis:
- ReturnCommand e ReturnCommandParameter
- SelectAllOnFocus
- GroupSeparatorMode