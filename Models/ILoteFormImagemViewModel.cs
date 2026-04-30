namespace SilvaData.Models;

public interface ILoteFormImagemViewModel
{
    int LoteFormId { get; }
    LoteFormImagem? LoteFormImagem1 { get; set; }
    LoteFormImagem? LoteFormImagem2 { get; set; }
    LoteFormImagem? LoteFormImagem3 { get; set; }
}
