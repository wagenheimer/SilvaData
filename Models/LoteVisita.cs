using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Utilities;

using Newtonsoft.Json;

using SQLite;

namespace SilvaData.Models
{
    public class UpdateDataParametrosLoteVisita : UpdateDataParametros
    {
        public new List<LoteVisita> array;
    }

    public class LoteVisita : ObservableObject
    {
        public static List<LoteVisita> VisitasAbertas;

        [PrimaryKey][AutoIncrement] public int DBId { get; set; }

        public int? id { get; set; }

        public int? idApp { get; set; }

        public int? lote { get; set; }

        public int? loteVisitaTipo { get; set; }

        public int? loteVisitaStatus { get; set; }

        public DateTime? dataInicio { get; set; }

        public DateTime? dataFim { get; set; }

        public DateTime dataUltimaAtualizacao { get; set; }

        public bool temmudanca { get; set; }

        [JsonIgnore][Ignore] public bool VisitaEstaFechada => loteVisitaStatus == 2;


        public static async Task<int> AtualizaVisita(LoteVisita loteVisita)
        {
            var update = await Db.UpdateAsync(loteVisita);

            WeakReferenceMessenger.Default.Send(new MudouVisitaMessage((int)loteVisita.lote));

            return update;
        }



        public static async Task<List<LoteVisita>> PegaListaVisitasAsync(int loteId)
        {
            var table = await Db.Table<LoteVisita>();

            return await table
                .Where(l => l.lote == loteId)
                .OrderByDescending(l => l.dataInicio)
                .ToListAsync();
        }

        public static async Task<LoteVisita> PegaVisitaAsync(int loteId, int visitaId)
        {
            var table = await Db.Table<LoteVisita>();
            return await table.Where(l => l.lote == loteId && l.id == visitaId).FirstOrDefaultAsync();
        }

    }
}