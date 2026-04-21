using Microsoft.Maui.Controls.Xaml;

#if DEBUG
// Desabilita compilação XAML em DEBUG para obter mensagens de erro detalhadas.
// Em produção (Release) a compilação XAML fica ativa para melhor performance.
[assembly: XamlCompilation(XamlCompilationOptions.Skip)]
#endif
