using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Hangfire.Job.Log
{
    public class Logger : IDisposable
    {

        #region Variables

        private readonly string _logName;
        private readonly LogEventLevel _logEventLevel = LogEventLevel.Fatal;
        private readonly IFormatProvider _formatLog;
        private readonly ILogger _logger;
        private readonly LoggerConfiguration _loggerConfiguration;

        #endregion

        #region Properties

        public ILogger WriteLog => _logger;

        #endregion

        #region Constructors

        internal Logger(string logName,
                        LoggerEventLevel logEventLevel)
        {
            _logName = logName;
            _logEventLevel = (LogEventLevel)logEventLevel;
            _loggerConfiguration = new LoggerConfiguration();

            var jobPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "jobs", logName);

            if (Directory.Exists(jobPath))
            {

                var jobSettingsFile = new ConfigurationBuilder().AddJsonFile(Path.Combine(jobPath, "jobsettings.json"), false).Build();

                if (jobSettingsFile != null)
                {
                    var logOptions = jobSettingsFile.GetSection("Assembly:LogMethod")
                                                    .GetChildren()
                                                    .Select(x => x.Value)
                                                    .ToArray();

                    foreach (var option in logOptions)
                    {
                        switch (option.ToLower())
                        {
                            case "file":

                                var jobLogPath = Path.Combine(jobPath, "log");

                                if (!Directory.Exists(jobLogPath))
                                    Directory.CreateDirectory(jobLogPath);

                                string logFileName = string.Format("{0}-.json", logName);

                                _loggerConfiguration.WriteTo
                                                    .File(formatter: new JsonFormatter(),
                                                          path: Path.Combine(jobLogPath, logFileName),
                                                          restrictedToMinimumLevel: _logEventLevel,
                                                          rollingInterval: RollingInterval.Day,
                                                          shared: true);

                                break;
                            case "sentry":

                                var sentryOptions = jobSettingsFile.GetSection("Sentry");

                                if (sentryOptions != null
                                 && !string.IsNullOrWhiteSpace(sentryOptions.GetSection("Dsn").Value))
                                {
                                    LogEventLevel mininumLevel = _logEventLevel;

                                    if (!string.IsNullOrWhiteSpace(sentryOptions.GetSection("LogLevel").Value))
                                        Enum.TryParse(sentryOptions.GetSection("LogLevel").Value, out mininumLevel);

                                    _loggerConfiguration.WriteTo
                                                        .Sentry(dsn: sentryOptions.GetSection("Dsn").Value,
                                                                release: sentryOptions.GetSection("Release").Value,
                                                                environment: sentryOptions.GetSection("Environment").Value,
                                                                restrictedToMinimumLevel: mininumLevel
                                                        );
                                }
                                break;
                        }
                    }

                }
            }

            _logger = _loggerConfiguration.Enrich.FromLogContext()
                                          .WriteTo
                                          .Console(restrictedToMinimumLevel: LogEventLevel.Verbose)
                                          .CreateLogger();

        }

        #endregion

        #region Methods

        public void Dispose()
        {
            Serilog.Log.Logger = _logger;
            Serilog.Log.CloseAndFlush();
        }

        #endregion

    }

    public enum LoggerEventLevel
    {
        //
        // Resumo:
        //     Anything and everything you might want to know about a running block of code.
        Verbose = 0,
        //
        // Resumo:
        //     Internal system events that aren't necessarily observable from the outside.
        Debug = 1,
        //
        // Resumo:
        //     The lifeblood of operational intelligence - things happen.
        Information = 2,
        //
        // Resumo:
        //     Service is degraded or endangered.
        Warning = 3,
        //
        // Resumo:
        //     Functionality is unavailable, invariants are broken or data is lost.
        Error = 4,
        //
        // Resumo:
        //     If you have a pager, it goes off when one of these occurs.
        Fatal = 5
    }
}
