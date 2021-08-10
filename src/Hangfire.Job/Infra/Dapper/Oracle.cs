using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Job.Infra.Dapper
{
    public class Oracle
    {

        #region Variables

        private readonly string _connectionString;
        private OracleConnection _connection;

        #endregion

        #region Properties

        #endregion

        #region Constructors

        public Oracle(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new OracleConnection(_connectionString);
        }

        #endregion

        #region Methods

        #endregion


    }
}
