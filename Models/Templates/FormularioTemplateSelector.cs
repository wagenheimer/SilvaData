using SilvaData.Models;
using Microsoft.Maui.Controls;

namespace ISIInstitute.Views.Templates
{
    public class FormularioTemplateSelector : DataTemplateSelector
    {
        #pragma warning disable CS0162 // Unreachable code detected — diagnostic bypass toggle
    private const bool DiagnosticBypassAlternativas = false;

        public DataTemplate Alternativas { get; set; }
        public DataTemplate ValorInteiro { get; set; }
        public DataTemplate CheckList { get; set; }
        public DataTemplate ISIMacro { get; set; }
        public DataTemplate ValorDouble { get; set; }
        public DataTemplate CampoTexto { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var parametro = item as ParametroComAlternativas;
            if (parametro == null)
                return null;

            Console.WriteLine($"[FORM_SELECTOR] ParametroId={parametro.id} Nome={parametro.nome} Tipo={parametro.tipoPreenchimento}");

            switch (parametro.tipoPreenchimento)
            {
                case TipoPreenchimento.Alternativas:
                    if (DiagnosticBypassAlternativas)
                    {
                        Console.WriteLine($"[FORM_SELECTOR] BYPASS Alternativas ParametroId={parametro.id} Nome={parametro.nome}");
                        return CreateDiagnosticTemplate("Alternativas");
                    }
                    return Alternativas;
                case TipoPreenchimento.ValorInteiro:
                    return ValorInteiro;
                case TipoPreenchimento.CheckList:
                    return CheckList;
                case TipoPreenchimento.ISIMacro:
                    return ISIMacro;
                case TipoPreenchimento.ValorDecimal:
                    return ValorDouble;
                case TipoPreenchimento.CampoTexto:
                    return CampoTexto;
                default:
                    Console.WriteLine("[FORM_SELECTOR] Unknown tipoPreenchimento: " + parametro.tipoPreenchimento);
                    return null;
            }
            #pragma warning restore CS0162
        }

        private static DataTemplate CreateDiagnosticTemplate(string tipo)
        {
            return new DataTemplate(() =>
            {
                var title = new Label
                {
                    FontSize = 12,
                    TextColor = Colors.DarkOrange,
                    Margin = new Thickness(10, 4, 10, 0)
                };
                title.SetBinding(Label.TextProperty, new Binding("nome", stringFormat: $"[DIAG:{tipo}] {{0}}"));

                var value = new Label
                {
                    FontSize = 11,
                    TextColor = Colors.Gray,
                    Margin = new Thickness(10, 0, 10, 8)
                };
                value.SetBinding(Label.TextProperty, new Binding("CategoriaParaAgrupar", stringFormat: "Categoria: {0}"));

                return new VerticalStackLayout
                {
                    Children = { title, value }
                };
            });
        }
    }
}
