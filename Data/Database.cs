using SQLite;

namespace SilvaData_MAUI.Models
{
    /// <summary>
    /// Gerencia a conexão singleton assíncrona com o banco de dados SQLite.
    /// </summary>
    public class Database
    {
        private static Database? _database;

        // Lock síncrono para a criação da instância
        private static readonly object _lockObject = new object();

        // Lock assíncrono para garantir que a inicialização ocorra apenas uma vez
        private static readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);

        private static bool _isInitialized = false;

        /// <summary>
        /// Conexão assíncrona com o banco de dados.
        /// </summary>
        public SQLiteAsyncConnection sqlConnection { get; private set; }

        /// <summary>
        /// Obtém o caminho completo para o arquivo de banco de dados no armazenamento local do aplicativo.
        /// </summary>
        public static string PathDB => Path.Combine(FileSystem.AppDataDirectory, "ISIDatabase.db3");

        /// <summary>
        /// Construtor privado para forçar o padrão singleton.
        /// </summary>
        /// <param name="dbPath">Caminho para o arquivo de banco de dados.</param>
        private Database(string dbPath)
        {
            // SharedCache removido: incompatível com WAL mode (causa serialização inesperada)
            sqlConnection = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        }

        /// <summary>
        /// Obtém a instância singleton do banco de dados, inicializando-a se necessário.
        /// Esta é a forma correta de acessar o banco de dados.
        /// </summary>
        /// <example>
        /// var db = await Database.GetInstanceAsync();
        /// var conexao = db.sqlConnection;
        /// </example>
        /// <returns>A instância do banco de dados inicializada.</returns>
        public static async Task<Database> GetInstanceAsync()
        {
            if (_database == null)
            {
                lock (_lockObject)
                {
                    // Double-check lock
                    _database ??= new Database(PathDB);
                }
            }

            // Garante que a inicialização (criação de tabelas) seja executada
            await _database.InitializeDatabaseAsync();

            return _database;
        }

        /// <summary>
        /// Inicializa o banco de dados (cria tabelas, etc.) de forma assíncrona e segura (thread-safe).
        /// </summary>
        /// <summary>
        /// Inicializa o banco de dados (configurações de conexão, etc.) 
        /// de forma assíncrona e segura (thread-safe).
        /// A criação de tabelas é gerenciada por 'ManutencaoTabelas'.
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            if (_isInitialized)
                return;

            await _asyncLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                // Apenas habilita o WAL. A criação de tabelas foi movida para ManutencaoTabelas.
                await sqlConnection.EnableWriteAheadLoggingAsync();

                _isInitialized = true;
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        /// <summary>
        /// Fecha a conexão com o banco de dados e limpa a instância singleton.
        /// </summary>
        public static async Task CloseDatabaseAsync()
        {
            if (_readConnection != null)
            {
                await _readConnection.CloseAsync();
                _readConnection = null;
            }

            if (_database?.sqlConnection != null)
            {
                await _database.sqlConnection.CloseAsync();
                lock (_lockObject)
                {
                    _database = null;
                    _isInitialized = false; // Permite reinicializar
                }
            }
        }

        /// <summary>
        /// Reabre o banco de dados. (Equivalente a chamar GetInstanceAsync).
        /// </summary>
        public static async Task ReopenDatabaseAsync()
        {
            // GetInstanceAsync já lida com a lógica de criação e inicialização
            await GetInstanceAsync();
        }

        /// <summary>
        /// Obtém a conexão de escrita pronta para uso.
        /// </summary>
        public static async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            var db = await GetInstanceAsync().ConfigureAwait(false);
            return db.sqlConnection;
        }

        // Conexão read-only separada da de escrita.
        // Com WAL ativo na conexão de escrita, o SQLite garante que leituras e escritas
        // em conexões distintas não se bloqueiam — readers não ficam na fila do writer.
        private static SQLiteAsyncConnection? _readConnection;

        /// <summary>
        /// Obtém a conexão read-only para queries de leitura da UI.
        /// </summary>
        public static async Task<SQLiteAsyncConnection> GetReadConnectionAsync()
        {
            if (_readConnection != null) return _readConnection;

            // Garante WAL habilitado antes de abrir a segunda conexão
            await GetInstanceAsync().ConfigureAwait(false);

            lock (_lockObject)
            {
                _readConnection ??= new SQLiteAsyncConnection(PathDB, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            }

            return _readConnection;
        }
    }
}