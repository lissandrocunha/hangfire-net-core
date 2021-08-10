using Hangfire.Job.Log;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Hangfire.Job.Core
{
    public abstract class HangfireJob : IDisposable
    {

        #region Variables

        private IDbConnection _dbConnection;
        private readonly string _jobName;
        private readonly Logger _logger;
        private readonly Dictionary<string, string> _connectionStrings;

        #endregion

        #region Properties

        /// <summary>
        /// Propriedade de conexão com o Banco de Dados configurada pelo métodos de Criação de Conexão (ex: CreateOracleConnection)
        /// </summary>
        protected IDbConnection DbConnection { get => _dbConnection; }
        /// <summary>
        /// Descrição do Nome do Job
        /// </summary>
        public string JobName { get => _jobName; }
        /// <summary>
        /// Propriedade para criar logs a partir do método WriteLog
        /// </summary>
        protected Logger Logger { get => _logger; }
        /// <summary>
        /// Dicionário de Strings de Conexão, onde key é o nome de identificação da string de conexão, e o value é a string de conexão
        /// </summary>
        public Dictionary<string, string> ConnectionStrings => _connectionStrings;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobName">Nome do Serviço</param>
        /// <param name="logEventLevel">Verbose: informações detalhadas de debug e rastreamento;
        ///                              Debug: informações de debug e fluxo da aplicação;
        ///                        Information: eventos de interesse e relevância;
        ///                            Warning: informações de possíveis problemas;
        ///                              Error: informações de falhas de qualquer tipo;
        ///                              Fatal: erros críticos que comprometam a aplicação de forma completa</param>
        protected HangfireJob(string jobName,
                                   LoggerEventLevel logEventLevel)
        {
            _jobName = jobName;
            _logger = new Logger(_jobName,
                                 logEventLevel);
            _connectionStrings = new Dictionary<string, string>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Método utilizado para iniciar o job
        /// </summary>
        public abstract void Execute();
        /// <summary>
        /// Método utilizado para execução de rotinas ao finalizar o job
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Método configura a variável DbConnection com uma coneção Oracle
        /// </summary>
        /// <param name="connectionString">String de conexão Oracle</param>
        protected void CreateOracleConnection(string connectionString)
        {
            _dbConnection = new OracleConnection(connectionString);
        }

        #endregion

    }
}
