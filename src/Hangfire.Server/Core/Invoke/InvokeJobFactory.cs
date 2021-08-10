using Hangfire.Job.Log;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hangfire.Server.Core.Invoke
{
    public class InvokeJobFactory : JobActivator, IDisposable
    {

        #region Variables

        private string _jobName;
        private string _jobFolder;

        #endregion

        #region Constructors

        public InvokeJobFactory()
        {

        }

        #endregion

        #region Methods

        [DisplayName("{0}")]
        public void Create(string jobName,
                           string jobFolder)
        {
            _jobName = jobName;
            _jobFolder = jobFolder;

            #region Carregar o arquivo de configuração do job

            var jsonSettings = new ConfigurationBuilder().AddJsonFile(Path.Combine(_jobFolder, "jobsettings.json"), false)
                                                         .Build();

            string hangfireJobName = string.IsNullOrWhiteSpace(jsonSettings.GetSection("Assembly:Nome").Value) ?
                                                               _jobName :
                                                               jsonSettings.GetSection("Assembly:Nome").Value;

            LoggerEventLevel logLevel = Enum.TryParse(typeof(LoggerEventLevel),
                                                      jsonSettings.GetSection("Assembly:LogLevel").Value, out object outLogLevel) ?
                                                      (LoggerEventLevel)outLogLevel :
                                                      LoggerEventLevel.Fatal;

            string dllAssembly = string.IsNullOrWhiteSpace(jsonSettings.GetSection("Assembly:DLL").Value) ?
                                                           "Hangfire.Job." + _jobName + ".dll" :
                                                           jsonSettings.GetSection("Assembly:DLL").Value;

            bool concurrentJob = string.IsNullOrWhiteSpace(jsonSettings.GetSection("Assembly:ConcurrentExecution").Value) ?
                                                           true :
                                                           Boolean.TryParse(jsonSettings.GetSection("Assembly:ConcurrentExecution").Value, out bool concurrentExecution) ? 
                                                           concurrentExecution : 
                                                           false;

            var args = new object[]
            {
                hangfireJobName,
                logLevel,
                concurrentJob
            };

            #endregion

            if (concurrentJob)
            {
                invokeAssembly(hangfireJobName,
                                   dllAssembly,
                                   args);
                return;
            }

            invokeAssemblyDisableConcurrentExecution(hangfireJobName,
                                            dllAssembly,
                                            args);
        }

        [DisplayName("{0} ({1})")]
        private void invokeAssembly(string hangfireJobName,
                                    string assemblyName,
                                    object[] args)
        {
            try
            {
                var path = Path.Combine(_jobFolder, assemblyName);

                #region Carregar o assembly

                if (!File.Exists(path))
                {
                    throw new Exception(string.Format("O assembly '{0}' não foi localizado.", assemblyName));
                }

                Assembly assembly = Assembly.LoadFrom(path);

                if (assembly.GetTypes().FirstOrDefault(x => x.Name == "BootStrapper")
                                       .BaseType.Name != "HangfireJob")
                {
                    throw new Exception(string.Format("O job '{0}' não é do tipo HangfireJob.", _jobName));
                }

                Type jobType = assembly.GetTypes().FirstOrDefault(x => x.Name == "BootStrapper");
                LoggerEventLevel eventLog = LoggerEventLevel.Error;
                Enum.TryParse(args[1].ToString(), out eventLog);
                Object classInstance = Activator.CreateInstance(jobType, args[0], eventLog);
                MethodInfo method = jobType.GetMethod("Execute");

                method.Invoke(classInstance, null);

                #endregion
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Format("Erro ao executar o job {0}.", _jobName));
                throw;
            }
        }


        [DisableConcurrentExecution(3600)]// 3600 = 1 hora
        [AutomaticRetry(Attempts = 0, LogEvents = false, OnAttemptsExceeded = AttemptsExceededAction.Delete, Order = 2)]
        [DisplayName("{0} ({1})")]
        private void invokeAssemblyDisableConcurrentExecution(string hangfireJobName,
                                                             string assemblyName,
                                                             object[] args)
        {
            try
            {
                var path = Path.Combine(_jobFolder, assemblyName);

                #region Carregar o assembly

                if (!File.Exists(path))
                {
                    throw new Exception(string.Format("O assembly '{0}' não foi localizado.", assemblyName));
                }

                Assembly assembly = Assembly.LoadFrom(path);

                if (assembly.GetTypes().FirstOrDefault(x => x.Name == "BootStrapper")
                                       .BaseType.Name != "HangfireJob")
                {
                    throw new Exception(string.Format("O job '{0}' não é do tipo HangfireJob.", _jobName));
                }

                #endregion

                #region Verifica se o job está em execução

                var connection = JobStorage.Current.GetConnection();

                var recurringJob = connection.GetRecurringJobs()
                                             .FirstOrDefault(j => j.Id == _jobName);

                if (recurringJob != null)
                {
                    var monitor = JobStorage.Current.GetMonitoringApi();
                    var processingJobs = monitor.ProcessingJobs(0, int.MaxValue)
                                                .Where(j => j.Value?.Job?.Args[0]?.ToString() == _jobName)
                                                .ToList();

                    //se só existe o próprio job em processamento, continua a execução
                    if (processingJobs.Count == 1)
                    {
                        Type jobType = assembly.GetTypes().FirstOrDefault(x => x.Name == "BootStrapper");
                        LoggerEventLevel eventLog = LoggerEventLevel.Error;
                        Enum.TryParse(args[1].ToString(), out eventLog);
                        Object classInstance = Activator.CreateInstance(jobType, args[0], eventLog);
                        MethodInfo method = jobType.GetMethod("Execute");

                        method.Invoke(classInstance, null);

                        return;
                    }


                }

                #endregion
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Format("Erro ao executar o job {0}.", _jobName));
                throw;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
