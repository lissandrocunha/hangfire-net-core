using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Monitor.Moldels;
using Hangfire.Monitor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hangfire.Monitor
{
    public class Worker : BackgroundService
    {

        #region Variables

        private readonly ILogger<Worker> _logger;
        private readonly ServiceConfigurations _serviceConfigurations;
        private readonly MonitorService _monitorService;

        #endregion

        #region Constructors

        public Worker(ILogger<Worker> logger,
                      IConfiguration configuration)
        {
            _logger = logger;

            _serviceConfigurations = new ServiceConfigurations();
            new ConfigureFromConfigurationOptions<ServiceConfigurations>(configuration.GetSection("ServiceConfigurations"))
                    .Configure(_serviceConfigurations);

            _monitorService = new MonitorService(_logger);
        }

        #endregion

        #region Methods

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {

                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1000, stoppingToken);

                    foreach (var windowsService in _serviceConfigurations.Services)
                    {
                        _logger.LogInformation(
                            $"Verificando a disponibilidade do serviço {windowsService.Nome}");

                        System.ServiceProcess.ServiceControllerStatus estadoServico;

                        Enum.TryParse(windowsService.Estado, out estadoServico);

                        _monitorService.VerifyServiceStatus(windowsService, estadoServico);
                    }

                    await Task.Delay(_serviceConfigurations.Intervalo, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO na execução do serviço");
            }
        }

        #endregion

    }
}
