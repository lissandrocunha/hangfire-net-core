using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Job.Core;
using Hangfire.Job.Log;
using Hangfire.Server.Core.Invoke;
using Hangfire.Server.ViewModels;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using static System.Net.Mime.MediaTypeNames;

namespace Hangfire.Server.Core
{
    public class JobManager
    {

        #region Variables

        private static IServiceProvider _serviceProvider;

        #endregion

        #region Constructors

        public JobManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Methods

        internal static int RestartHangfireServer()
        {
            try
            {
                string applicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var jobPath = Path.Combine(applicationFolder, "jobs");
                var batchFile = Path.Combine(jobPath, "restart_service.cmd");


                int exitCode;
                ProcessStartInfo processInfo;
                Process process;

                processInfo = new ProcessStartInfo();
                processInfo.CreateNoWindow = true; //This hides the dos-style black window that the command prompt usually shows
                processInfo.FileName = @"cmd.exe";
                processInfo.Verb = "runas"; //This is what actually runs the command as administrator
                processInfo.Arguments = "/C " + batchFile;
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                // *** Redirect the output ***
                processInfo.RedirectStandardError = true;
                processInfo.RedirectStandardOutput = true;

                process = Process.Start(processInfo);
                process.WaitForExit();

                // *** Read the streams ***
                // Warning: This approach can lead to deadlocks, see Edit #2
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                exitCode = process.ExitCode;

                process.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao tentar reiniciar o serviço do windows do Hangfire.");
                return StatusCodes.Status500InternalServerError;
            }

            return StatusCodes.Status200OK;
        }

        internal static void StartRecurringJobs()
        {
            string applicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var jobPath = Path.Combine(applicationFolder, "jobs");

            if (!Directory.Exists(jobPath)) return;

            foreach (var job in Directory.GetDirectories(jobPath))
            {
                try
                {
                    var jsonSettings = new ConfigurationBuilder().AddJsonFile(path: Path.Combine(job, "jobsettings.json"),
                                                                      optional: false,
                                                                      reloadOnChange: true)
                                                         .Build();

                    if (jsonSettings == null) continue;

                    if (bool.TryParse(jsonSettings["Assembly:RecurringJob"], out bool recurringJob)
                     && recurringJob)
                    {
                        TaskJob(job.Split('\\').LastOrDefault());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Erro ao carregar o Job " + job + ".");
                    continue;
                }
            }
        }

        internal static List<string> GetInstalledJobs()
        {
            string applicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var pathJobFolders = Path.Combine(applicationFolder, "jobs");

            return Directory.GetDirectories(pathJobFolders)
                            .Select(x => x.Replace(pathJobFolders, "").Replace("\\", ""))
                            .ToList();
        }

        internal static int DeleteJob(string jobName)
        {

            try
            {
                string applicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var pathJobFolders = Path.Combine(applicationFolder, "jobs", jobName);

                if (!Directory.Exists(pathJobFolders))
                {
                    Log.Error("O Job " + jobName + " não foi localizado.");
                    return StatusCodes.Status404NotFound;
                }

                #region Excluir Job em Execução

                using (var connection = JobStorage.Current.GetConnection())
                {
                    var recurringJob = connection.GetRecurringJobs()
                                        .FirstOrDefault(j => j.Id == jobName);
                    if (recurringJob != null)
                    {
                        RecurringJob.RemoveIfExists(recurringJob.Id);
                    }
                }

                var monitor = JobStorage.Current.GetMonitoringApi();
                List<EnqueuedJobDto> jobsAgendados = new List<EnqueuedJobDto>();

                foreach (var queue in monitor.Queues())
                {
                    monitor.EnqueuedJobs(queue.Name, 0, int.MaxValue)
                           .Where(j => j.Value.Job.Method.Name == jobName)
                           .ToList()
                           .ForEach(x => jobsAgendados.Add(x.Value));
                }

                var processingJobs = monitor.ProcessingJobs(0, int.MaxValue)
                                           .Where(j => j.Value?.Job?.Args[0]?.ToString() == jobName)
                                           .ToList();

                foreach (var processingJob in processingJobs)
                {
                    BackgroundJob.Delete(processingJob.Key);
                }

                var scheduledJobs = monitor.ScheduledJobs(0, int.MaxValue)
                                           .Where(j => j.Value?.Job?.Args[0]?.ToString() == jobName)
                                           .ToList();

                foreach (var scheduledJob in scheduledJobs)
                {
                    BackgroundJob.Delete(scheduledJob.Key);
                }

                #endregion              

                #region Excluir Arquivos do Job

                DeleteJobFolder(pathJobFolders);

                #endregion
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "BackgroundJob.Delete");
                return StatusCodes.Status500InternalServerError;
            }


            return StatusCodes.Status200OK;
        }

        public static void DeleteJobFolder(string path)
        {
            try
            {
                DeleteDirectory(path);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static void DeleteDirectory(string directory)
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            foreach (var subDirectory in Directory.GetDirectories(directory))
            {
                DeleteDirectory(subDirectory);
            }

            Directory.Delete(directory, true);
        }

        internal static void FireAndForgetJob(string jobName)
        {
            try
            {
                string applicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var jobPath = Path.Combine(applicationFolder, "jobs");
                var jobFolder = Path.Combine(jobPath, jobName);

                if (!Directory.Exists(jobFolder))
                {
                    Log.Error("O Job " + jobName + " não foi localizado.");
                    return;
                }

                BackgroundJob.Enqueue(() => new InvokeJobFactory().Create(jobName, jobFolder));

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao executar a rotina FireAndForgetJob.");
            }
        }

        internal static void ScheduleJob(string jobName, DateTime dateTime)
        {
            try
            {
                TimeSpan schedule = TimeSpan.FromTicks(dateTime.Ticks - DateTime.Now.Ticks);

                string applicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var jobPath = Path.Combine(applicationFolder, "jobs");
                var jobFolder = Path.Combine(jobPath, jobName);

                if (!Directory.Exists(jobFolder))
                {
                    Log.Error("O Job " + jobName + " não foi localizado.");
                    return;
                }

                BackgroundJob.Schedule(() => new InvokeJobFactory().Create(jobName, jobFolder), schedule);

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao executar a rotina DelayedJob.");
            }
        }

        internal static void TaskJob(string jobName)
        {
            try
            {
                string applicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var jobPath = Path.Combine(applicationFolder, "jobs");
                var jobFolder = Path.Combine(jobPath, jobName);

                if (!Directory.Exists(jobFolder))
                {
                    Log.Error("O Job " + jobName + " não foi localizado.");
                    return;
                }

                #region Configurações do Job

                var jsonSettings = new ConfigurationBuilder().AddJsonFile(Path.Combine(jobFolder, "jobsettings.json"), false)
                                             .Build();

                string cronTask = string.IsNullOrWhiteSpace(jsonSettings.GetSection("Assembly:CRON").Value) ?
                                            " 1 * * * * " :
                                            jsonSettings.GetSection("Assembly:CRON").Value;

                string groupTask = string.IsNullOrWhiteSpace(jsonSettings.GetSection("Assembly:Queue").Value) ?
                                                            "default" :
                                                            jsonSettings.GetSection("Assembly:Queue").Value.ToLower();

                #endregion

                RecurringJob.AddOrUpdate(jobName,
                                         () => new InvokeJobFactory().Create(jobName, jobFolder),
                                         cronTask,
                                         TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"),
                                         groupTask);

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao executar a rotina TaskJob.");
            }

        }

        internal static object UpdateJobAssembly(JobAssemblyInfoViewModel viewModel)
        {
            object info = null;

            #region Carregar o arquivo de configuração do job

            string applicationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var jobPath = Path.Combine(applicationFolder, "jobs");
            var jobFolder = Path.Combine(jobPath, viewModel.JobName);

            var jsonSettings = new ConfigurationBuilder().AddJsonFile(Path.Combine(jobFolder, "jobsettings.json"), false, true)
                                                         .Build();

            if (jsonSettings == null)
                throw new Exception("Não foi possível carregar o arquivo de configuração do job " + viewModel.JobName + " na rotina UpdateJobAssembly.");

            if (!string.IsNullOrWhiteSpace(viewModel.LogLevel)
             && jsonSettings.GetSection("Assembly:LogLevel") != null)
                jsonSettings.GetSection("Assembly:LogLevel").Value = viewModel.LogLevel;

            if (!string.IsNullOrWhiteSpace(viewModel.DllJob)
             && jsonSettings.GetSection("Assembly:DLL") != null)
                jsonSettings.GetSection("Assembly:DLL").Value = viewModel.DllJob;

            if (!string.IsNullOrWhiteSpace(viewModel.Cron)
             && jsonSettings.GetSection("Assembly:CRON") != null)
                jsonSettings.GetSection("Assembly:CRON").Value = viewModel.Cron;

            if (!string.IsNullOrWhiteSpace(viewModel.Queue)
             && jsonSettings.GetSection("Assembly:Queue") != null)
                jsonSettings.GetSection("Assembly:Queue").Value = viewModel.Queue;

            if (viewModel.LogMetod.Count() > 0
             && jsonSettings.GetSection("Assembly:LogMethod") != null)
                jsonSettings.GetSection("Assembly:LogMethod").Value = string.Join(',', viewModel.LogMetod);

            if (jsonSettings.GetSection("Assembly:RecurringJob") != null)
                jsonSettings.GetSection("Assembly:RecurringJob").Value = viewModel.RecurringJob.ToString();


            #endregion

            info = JsonConvert.SerializeObject(jsonSettings);

            return info;
        }

        #endregion

    }
}
