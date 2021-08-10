using Hangfire.Job.Infra.Dapper.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Hangfire.Job.Infra.Dapper
{
    public sealed class UnitOfWork : IUnitOfWork
    {

        #region Variables

        private readonly Guid _id = Guid.Empty;
        private readonly IDbConnection _connection = null;
        private IDbTransaction _transaction = null;
        private readonly Dictionary<Type, object> _repositories;

        #endregion

        #region Properties

        public Guid Id => _id;
        public IDbConnection Connection => _connection;

        public IDbTransaction Transaction => _transaction;

        #endregion

        #region Constructors

        public UnitOfWork(IDbConnection connection)
        {
            _id = Guid.NewGuid();
            _connection = connection;
            _repositories = new Dictionary<Type, object>();
        }

        #endregion

        #region Methods

        public void Begin()
        {
            _transaction = _connection.BeginTransaction();
        }

        public void Commit()
        {
            try
            {
                _transaction.Commit();
                _transaction.Connection?.Close();
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
            finally
            {
                Dispose();
            }
        }

        public void Rollback()
        {
            try
            {
                _transaction.Rollback();
                _transaction.Connection?.Close();
            }
            catch
            {
                throw;
            }
            finally
            {
                Dispose();
            }

        }

        public T Repository<T>() where T : class
        {
            if (_repositories.Keys.Contains(typeof(T)))
                return _repositories[typeof(T)] as T;

            var iType = typeof(T);

            var sType = AppDomain.CurrentDomain.GetAssemblies()
                                 .SelectMany(x => x.GetTypes())
                                 .FirstOrDefault(el => !el.IsInterface && iType.IsAssignableFrom(el));

            var repo = (T)Activator.CreateInstance(sType, _connection);

            _repositories.Add(typeof(T), repo);

            return repo;
        }

        public void Dispose()
        {
            if (Transaction != null)
                Transaction.Dispose();
            _transaction = null;
        }



        #endregion
    }
}
