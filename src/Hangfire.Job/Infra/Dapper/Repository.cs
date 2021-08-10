using Dapper;
using Hangfire.Job.Infra.Dapper.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Job.Infra.Dapper
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {

        #region Variables

        private readonly string _tableName;
        private readonly IDbConnection _connection;

        #endregion

        #region Properties

        public IDbConnection Connection => _connection;

        private IEnumerable<PropertyInfo> GetProperties => typeof(TEntity).GetProperties();

        #endregion

        #region Constructors

        public Repository(IDbConnection connection,
                          string tableName)
        {
            _connection = connection;
            _tableName = tableName;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Open new connection and return it for use
        /// </summary>
        /// <returns></returns>
        private IDbConnection CreateConnection()
        {
            return Activator.CreateInstance(_connection.GetType()) as IDbConnection;
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<TEntity>($"SELECT * FROM {_tableName}");
            }
        }

        public async Task InsertAsync(TEntity t)
        {
            var insertQuery = GenerateInsertQuery();

            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(insertQuery, t);
            }
        }

        public async Task UpdateAsync(TEntity t)
        {
            var updateQuery = GenerateUpdateQuery();

            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(updateQuery, t);
            }
        }

        public async Task<int> SaveRangeAsync(IEnumerable<TEntity> list)
        {
            var inserted = 0;
            var query = GenerateInsertQuery();
            using (var connection = CreateConnection())
            {
                inserted += await connection.ExecuteAsync(query, list);
            }

            return inserted;
        }

        private static List<string> GenerateListOfProperties(IEnumerable<PropertyInfo> listOfProperties)
        {
            return (from prop in listOfProperties
                    let attributes = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    where attributes.Length <= 0 || (attributes[0] as DescriptionAttribute)?.Description != "ignore"
                    select prop.Name).ToList();
        }

        private string GenerateInsertQuery()
        {
            var insertQuery = new StringBuilder($"INSERT INTO {_tableName} ");

            insertQuery.Append("(");

            var properties = GenerateListOfProperties(GetProperties);
            properties.ForEach(prop => { insertQuery.Append($"[{prop}],"); });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(") VALUES (");

            properties.ForEach(prop => { insertQuery.Append($"@{prop},"); });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(")");

            return insertQuery.ToString();
        }

        private string GenerateUpdateQuery()
        {
            var updateQuery = new StringBuilder($"UPDATE {_tableName} SET ");
            var properties = GenerateListOfProperties(GetProperties);

            properties.ForEach(property =>
            {
                if (!property.Equals("Id"))
                {
                    updateQuery.Append($"{property}=:{property},");
                }
            });

            updateQuery.Remove(updateQuery.Length - 1, 1); //remove last comma
            updateQuery.Append(" WHERE Id=:Id");

            return updateQuery.ToString();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }


        #endregion

    }
}
