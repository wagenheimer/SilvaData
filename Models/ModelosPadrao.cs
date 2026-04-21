using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SilvaData.Models
{
    public class Tabela
    {
        public string name { get; set; }
        public string tbl_name { get; set; }

        public static async Task<List<Tabela>> GetTabelasAsync()
        {
            return await Db.QueryAsync<Tabela>("SELECT * FROM sqlite_master WHERE type = 'table'");
        }

        internal async Task DropTable(Tabela tabela)
        {
            if (tabela.tbl_name != "sqlite_sequence")
                await Db.ExecuteAsync($"DROP TABLE {tabela.tbl_name}");
        }
    }


}
