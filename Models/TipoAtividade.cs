using CommunityToolkit.Mvvm.ComponentModel;

using SQLite;

namespace SilvaData.Models
{
    public partial class TipoCorAtividade : ObservableObject
    {
        private int? _id;

        [PrimaryKey]
        public int? id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _nome;

        public string nome
        {
            get => _nome;
            set => SetProperty(ref _nome, value);
        }

        public static async Task<List<TipoCorAtividade>> GetItemsAsync()
        {
            var table = await Db.Table<TipoCorAtividade>();
            return await table.ToListAsync();
        }
    }

    public class TipoCorAtividadesFromWebService
    {
        public List<TipoCorAtividade> TipoCorAtividades;
    }

    public partial class TipoAtividade : ObservableObject
    {
        private int? _id;
        [PrimaryKey]
        public int? id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _nome;
        public string nome
        {
            get => _nome;
            set => SetProperty(ref _nome, value);
        }

        private int? _status;
        public int? status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private int? _atividadeTipoCorId;
        public int? atividadeTipoCorId
        {
            get => _atividadeTipoCorId;
            set => SetProperty(ref _atividadeTipoCorId, value);
        }

        private DateTime _dataUltimaAtualizacao;
        public DateTime dataUltimaAtualizacao
        {
            get => _dataUltimaAtualizacao;
            set => SetProperty(ref _dataUltimaAtualizacao, value);
        }

        private int? _excluido;
        public int? excluido
        {
            get => _excluido;
            set => SetProperty(ref _excluido, value);
        }

        public static async Task<List<TipoAtividade>> GetItemsAsync()
        {
            var table = await Db.Table<TipoAtividade>();
            return await table.Where(a => a.excluido == null).ToListAsync();
        }
    }

    public class TipoAtividadesFromWebService
    {
        public List<TipoAtividade> TipoAtividades;
    }
}