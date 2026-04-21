using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

using SQLite;

namespace SilvaData.Models
{
    /// <summary>
    /// Fachada estática para simplificar o acesso assíncrono ao banco de dados.
    /// Substitui o padrão 'Db.XXX'
    /// e centraliza a obtenção da conexão.
    /// </summary>
    public static class Db
    {
        // Conexão de escrita — usada por INSERT, UPDATE, DELETE, ExecuteAsync, RunInTransactionAsync
        private static async Task<SQLiteAsyncConnection> GetDb()
        {
            return await Database.GetConnectionAsync();
        }

        // Conexão read-only — usada por SELECT, Table<T>, FindAsync, etc.
        // Conexão separada da de escrita para não ficar na fila do SyncService (WAL garante concorrência).
        private static async Task<SQLiteAsyncConnection> GetReadDb()
        {
            return await Database.GetReadConnectionAsync();
        }

        #region CRUD (Create, Read, Update, Delete)

        /// <summary>
        /// Insere um novo item no banco de dados.
        /// </summary>
        public static async Task<int> InsertAsync(object item)
        {
            var db = await GetDb();
            return await db.InsertAsync(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Insere um item. Se já existir (baseado na Chave Primária), ele será substituído.
        /// </summary>
        public static async Task<int> InsertOrReplaceAsync(object item)
        {
            var db = await GetDb();
            return await db.InsertOrReplaceAsync(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Atualiza um item existente no banco de dados.
        /// </summary>
        public static async Task<int> UpdateAsync(object item)
        {
            var db = await GetDb();
            return await db.UpdateAsync(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Deleta um item do banco de dados.
        /// </summary>
        public static async Task<int> DeleteAsync(object item)
        {
            var db = await GetDb();
            return await db.DeleteAsync(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Deleta um item por sua chave primária.
        /// </summary>
        public static async Task<int> DeleteAsync<T>(object primaryKey)
        {
            var db = await GetDb();
            return await db.DeleteAsync<T>(primaryKey).ConfigureAwait(false);
        }

        #endregion

        #region Consultas (Query)

        /// <summary>
        /// Executa uma consulta SQL bruta e retorna uma lista de objetos.
        /// </summary>
        public static async Task<List<T>> QueryAsync<T>(string query, params object[] args) where T : new()
        {
            var db = await GetReadDb();
            return await db.QueryAsync<T>(query, args).ConfigureAwait(false);
        }

        /// <summary>
        /// Executa um comando SQL bruto (não-consulta, ex: UPDATE, DELETE).
        /// </summary>
        public static async Task<int> ExecuteAsync(string query, params object[] args)
        {
            var db = await GetDb();
            return await db.ExecuteAsync(query, args).ConfigureAwait(false);
        }

        /// <summary>
        /// Retorna uma referência à tabela para construir consultas LINQ.
        /// </summary>
        public static async Task<AsyncTableQuery<T>> Table<T>() where T : new()
        {
            var db = await GetReadDb();
            return db.Table<T>();
        }

        public static async Task<T> FindAsync<T>(object primaryKey) where T : new()
        {
            var db = await GetReadDb();
            return await db.FindAsync<T>(primaryKey).ConfigureAwait(false);
        }

        public static async Task<T> FindAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            var db = await GetReadDb();
            return await db.FindAsync<T>(predicate).ConfigureAwait(false);
        }

        public static async Task<T> FindWithQueryAsync<T>(string query, params object[] args) where T : new()
        {
            var db = await GetReadDb();
            return await db.FindWithQueryAsync<T>(query, args).ConfigureAwait(false);
        }

        public static async Task<T> GetFirstAsync<T>() where T : new()
        {
            var db = await GetReadDb();
            return await db.Table<T>().FirstOrDefaultAsync();
        }

        /// <summary>
        /// Executa uma ação síncrona dentro de uma transação de banco de dados.
        /// A ação recebe uma conexão SÍNCRONA.
        /// Se a ação falhar (lançar exceção), a transação fará rollback.
        /// </summary>
        /// <param name="action">A ação síncrona a ser executada (que recebe um SQLiteConnection síncrono).</param>
        public static async Task RunInTransactionAsync(Action<SQLiteConnection> action)
        {
            var db = await GetDb(); // GetDb() retorna SQLiteAsyncConnection
            await db.RunInTransactionAsync(action).ConfigureAwait(false);
        }
        #endregion
    }
}
