using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Utilities;

using Newtonsoft.Json;

using SQLite;

namespace SilvaData.Models
{
    public class UpdateDataParametrosUnidadeEpidemiologica : UpdateDataParametros
    {
        [JsonProperty("array")]
        public new List<UnidadeEpidemiologicaUploadDto>? Array { get; set; }
    }

    /// <summary>
    /// DTO para envio de Unidade Epidemiológica para o servidor.
    /// Garante compatibilidade com o contrato do backend sem usar substituição de strings.
    /// </summary>
    public class UnidadeEpidemiologicaUploadDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("idApp")]
        public int? IdApp { get; set; }

        [JsonProperty("nome")]
        public string? Nome { get; set; }

        [JsonProperty("latitude")]
        public double? Latitude { get; set; }

        [JsonProperty("longitude")]
        public double? Longitude { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("propriedade")]
        public int? PropriedadeId { get; set; }

        [JsonProperty("excluido")]
        public int? Excluido { get; set; }

        [JsonProperty("parametros")]
        public List<ParametroUploadDto>? Parametros { get; set; }
    }

    /// <summary>
    /// DTO para envio de parâmetros da Unidade Epidemiológica.
    /// </summary>
    public class ParametroUploadDto
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("valor")]
        public string? Valor { get; set; }
    }

    public partial class UnidadeEpidemiologicaParametro : ParametrosValores
    {
        public int? unidadeEpidemiologicaId { get; set; }

        public static async Task<List<UnidadeEpidemiologicaParametro>> GetItemsAsync(int id)
        {
            var table = await Db.Table<UnidadeEpidemiologicaParametro>();
            return await table.Where(p => p.unidadeEpidemiologicaId == id).ToListAsync();
        }

        public static Task<int> SaveItemAsync(UnidadeEpidemiologicaParametro uep)
        {
            return Db.InsertOrReplaceAsync(uep);
        }

        /// <summary>
        /// Obtém os parâmetros formatados para upload.
        /// </summary>
        internal static async Task<List<ParametroUploadDto>> GetItemsForUploadAsync(int? id)
        {
            var table = await Db.Table<UnidadeEpidemiologicaParametro>();
            var parlist = await table.Where(p => p.unidadeEpidemiologicaId == id).ToListAsync();

            var result = new List<ParametroUploadDto>();
            foreach (var par in parlist)
            {
                result.Add(new ParametroUploadDto { Id = par.parametroId, Valor = par.valor });
            }
            return result;
        }
    }

    public partial class UnidadeEpidemiologica : ObservableObject
    {
        // REMOVIDO: public static List<UnidadeEpidemiologica> UnidadeEpidemiologicasList

        private int _id;
        [PrimaryKey]
        public int id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private int? _idApp;
        public int? idApp
        {
            get => _idApp;
            set => SetProperty(ref _idApp, value);
        }

        private string? _nome;
        public string? nome
        {
            get => _nome;
            set => SetProperty(ref _nome, value);
        }

        private double? _latitude;
        [JsonProperty("lat")]
        public double? latitude
        {
            get => _latitude;
            set => SetProperty(ref _latitude, value);
        }

        private double? _longitude;
        [JsonProperty("lng")]
        public double? longitude
        {
            get => _longitude;
            set => SetProperty(ref _longitude, value);
        }

        private int _status;
        public int status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private int? _propriedadeId;
        [JsonProperty("propriedadeId")]
        public int? propriedadeId
        {
            get => _propriedadeId;
            set => SetProperty(ref _propriedadeId, value);
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

        private bool _temmudanca;
        public bool temmudanca
        {
            get => _temmudanca;
            set => SetProperty(ref _temmudanca, value);
        }

        // REMOVIDO: Propriedades computadas que dependiam de DadosStatic
        // Agora serão resolvidas no ViewModel ou CacheService

        /// <summary>
        /// Retorna a lista de Unidades Epidemiológicas ativas.
        /// </summary>
        public static async Task<List<UnidadeEpidemiologica>> GetActiveListAsync()
        {
            var table = await Db.Table<UnidadeEpidemiologica>().ConfigureAwait(false);

            return await table
                .Where(p => p.status == 1 && (p.excluido == null || p.excluido != 1))
                .OrderBy(u => u.nome)
                .ToListAsync().ConfigureAwait(false);
        }

        public static async Task<UnidadeEpidemiologica> GetItemAsync(int id)
        {
            var table = await Db.Table<UnidadeEpidemiologica>().ConfigureAwait(false);
            return await table.Where(i => i.id == id).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Salva ou atualiza uma Unidade Epidemiológica e notifica o CacheService.
        /// </summary>
        public static async Task SaveAsync(UnidadeEpidemiologica item)
        {
            item.dataUltimaAtualizacao = DateTime.Now;
            item.temmudanca = true;
            item.status = 1;

            if (item.id != 0)
            {
                await Db.UpdateAsync(item).ConfigureAwait(false);
            }
            else
            {
                item.id = await Alteracao.GetNextId(item).ConfigureAwait(false);
                await Db.InsertAsync(item).ConfigureAwait(false);
            }

            // Notifica o CacheService para atualizar
            WeakReferenceMessenger.Default.Send(new RefreshCacheMessage(CacheType.UnidadesEpidemiologicas));
        }

        public Task<int> DeleteItemAsync(UnidadeEpidemiologica item)
        {
            return Db.DeleteAsync(item);
        }

        /// <summary>
        /// Sincroniza as alterações pendentes das Unidades Epidemiológicas com o servidor.
        /// Realiza o upload e atualiza os IDs locais com base na resposta do backend.
        /// </summary>
        internal static async Task SyncPendingChangesToServerAsync()
        {
            try
            {
                var sql = Alteracao.SqlNovosDados("unidadeepidemiologica");
                var alteracoes = await Db.QueryAsync<UnidadeEpidemiologica>(sql).ConfigureAwait(false);

                if (alteracoes.Count == 0) return;

                var listToUpload = new List<UnidadeEpidemiologicaUploadDto>();

                foreach (var item in alteracoes)
                {
                    var dto = new UnidadeEpidemiologicaUploadDto
                    {
                        Id = (item.idApp == 0 || item.idApp == null) ? -1 : item.id,
                        IdApp = (item.idApp == 0 || item.idApp == null) ? item.id : item.idApp,
                        Nome = item.nome,
                        Latitude = item.latitude,
                        Longitude = item.longitude,
                        Status = item.status,
                        PropriedadeId = item.propriedadeId,
                        Excluido = item.excluido,
                        Parametros = await UnidadeEpidemiologicaParametro.GetItemsForUploadAsync(item.id).ConfigureAwait(false)
                    };
                    listToUpload.Add(dto);
                }

                var updateData = new UpdateDataParametrosUnidadeEpidemiologica { Array = listToUpload };
                var updateJson = JsonConvert.SerializeObject(updateData);
                updateJson = Alteracao.AjustaArray(updateJson);

                var result = await ISIWebService.Instance.SendData(updateJson, "postUnidadesEpidemiologicas").ConfigureAwait(false);

                if (result.sucesso)
                {
                    result.data = result.data.Replace("unidadesEpidemiologicas", "dados");
                    result.data = Alteracao.AjustaResultData(result.data);
                    var resultIds = JsonConvert.DeserializeObject<UpdateResults>(result.data);

                    if (resultIds?.dados == null) return;

                    await Db.RunInTransactionAsync(conn =>
                    {
                        foreach (var resultinfo in resultIds.dados)
                        {
                            // Garante que o idApp esteja correto no banco local (seja novo ou existente)
                            conn.Execute("update UnidadeEpidemiologica set idApp=? where id=?", resultinfo.idApp, resultinfo.idApp);

                            if (resultinfo.id != resultinfo.idApp)
                            {
                                // Sincroniza o ID definitivo do servidor e limpa flags de mudança
                                conn.Execute("update UnidadeEpidemiologica set id=?, idApp=NULL, temmudanca=0 where id=?", resultinfo.id, resultinfo.idApp);
                                conn.Execute("update UnidadeEpidemiologicaParametro set unidadeEpidemiologicaId=? where unidadeEpidemiologicaId=?", resultinfo.id, resultinfo.idApp);
                                conn.Execute("update Lote set unidadeEpidemiologicaId=? where unidadeEpidemiologicaId=?", resultinfo.id, resultinfo.idApp);
                            }
                            else
                            {
                                // Apenas limpa a flag de mudança se o ID já for o definitivo
                                conn.Execute("update UnidadeEpidemiologica set temmudanca=0 where id=?", resultinfo.id);
                            }
                        }
                    }).ConfigureAwait(false);
                }
                else
                {
                    await SentryHelper.LogErrorAsync("Unidades Epidemiológicas", "UnidadeEpidemiologica", result.mensagem).ConfigureAwait(false);
                    throw new Exception(!string.IsNullOrEmpty(result.mensagem) ? result.mensagem : "Erro desconhecido ao enviar unidades epidemiológicas");
                }
            }
            catch (Exception ex)
            {
                await SentryHelper.LogErrorAsync("SyncPendingChangesToServerAsync UnidadeEpidemiologica", "UnidadeEpidemiologica", ex.ToString()).ConfigureAwait(false);
                throw;
            }
        }
    }

    public partial class UnidadeEpidemiologicaComDetalhes : UnidadeEpidemiologica
    {
        public string? RegionalNome { get; set; }
        public string? PropriedadeNome { get; set; }
        public int QuantidadeLotes { get; set; }

        /// <summary>
        /// Retorna a lista de Unidades Epidemiológicas com detalhes de Regional e Propriedade.
        /// </summary>
        public static async Task<List<UnidadeEpidemiologicaComDetalhes>> GetListWithDetailsAsync()
        {
            const string sql = @"
                SELECT ue.*,
                       p.nome AS PropriedadeNome,
                       r.nome AS RegionalNome,
                       (SELECT COUNT(*) FROM Lote l WHERE l.unidadeEpidemiologicaId = ue.id AND COALESCE(l.excluido, 0) = 0) AS QuantidadeLotes
                FROM UnidadeEpidemiologica ue
                LEFT OUTER JOIN Propriedade p ON p.id = ue.propriedadeId
                LEFT OUTER JOIN Regional r ON r.id = p.regionalId
                WHERE COALESCE(ue.excluido, 0) = 0 AND ue.status = 1
                ORDER BY ue.nome, r.nome, p.nome";

            return await Db.QueryAsync<UnidadeEpidemiologicaComDetalhes>(sql).ConfigureAwait(false);
        }
    }

    public class UnidadeEpidemiologicaFromWebService : UnidadeEpidemiologica
    {
        [JsonProperty("parametros")]
        public List<ParametrosValores>? parametros { get; set; }
    }

    public class UnidadesEpidemiolgicasFromWebService
    {
        [JsonProperty("unidadesEpidemiologicas")]
        public List<UnidadeEpidemiologicaFromWebService>? unidadesEpidemiologicas { get; set; }
    }
}
