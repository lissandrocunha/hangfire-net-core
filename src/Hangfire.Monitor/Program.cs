using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;

namespace Hangfire.Monitor
{
    public class Program
    {
        #region Variables

        private static IHostEnvironment _environment;

        #endregion

        #region Constructors

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        #endregion

        #region Methods

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    _environment = hostingContext.HostingEnvironment;

                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{_environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

                    if (_environment.IsDevelopment())
                    {
                        var appAssembly = Assembly.Load(new AssemblyName(_environment.ApplicationName));
                        if (appAssembly != null)
                        {
                            config.AddUserSecrets(appAssembly, optional: true);
                        }
                    }

                    config.AddEnvironmentVariables();

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureLogging((hostContext, logginBuilder) =>
                {
                    logginBuilder.Services.Configure<SentryLoggingOptions>(hostContext.Configuration.GetSection("Sentry"));
                    logginBuilder.AddSentry(options =>
                    {
                        options.ServerName = "Hangfire - Server Monitor";
                        options.Environment = _environment.EnvironmentName;
                    });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });

        #endregion
    }
}
