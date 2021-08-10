using Hangfire.Monitor.Moldels;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Hangfire.Monitor.Services
{
    public class MonitorService
    {

        #region Variables

        private readonly ILogger _logger;

        #endregion

        #region Constructors

        public MonitorService(ILogger logger)
        {
            _logger = logger;
        }

        #endregion

        #region Methods

        internal void VerifyServiceStatus(WindowsServiceConfiguration serviceName, ServiceControllerStatus status)
        {

            var windowsService = ServiceController.GetServices()
                                                  .FirstOrDefault(s => s.ServiceName == serviceName.Nome);

            if (windowsService == null)
            {
                _logger.LogError(string.Format("O serviço {0} não foi encontrado ", serviceName));
                return;
            }

            _logger.LogInformation("");
            _logger.LogInformation(JsonConvert.SerializeObject(windowsService));
            _logger.LogInformation("");

            if (windowsService.Status != status)
            {
                SetServiceControllerStatus(windowsService, status);
                return;
            }

            _logger.LogInformation(string.Format("O serviço {0} já está com o estado {1}.", windowsService.ServiceName, status.ToString()));
        }

        private void SetServiceControllerStatus(ServiceController windowsService, ServiceControllerStatus status)
        {
            try
            {
                switch (status)
                {
                    case ServiceControllerStatus.Stopped:
                        windowsService.Stop();
                        break;
                    case ServiceControllerStatus.StartPending:
                        windowsService.WaitForStatus(status, new TimeSpan(0, 0, 10));
                        windowsService.Start();
                        break;
                    case ServiceControllerStatus.StopPending:
                        windowsService.WaitForStatus(status, new TimeSpan(0, 0, 10));
                        windowsService.Stop();
                        break;
                    case ServiceControllerStatus.Running:
                        windowsService.Start();
                        break;
                    case ServiceControllerStatus.ContinuePending:
                        break;
                    case ServiceControllerStatus.PausePending:
                        break;
                    case ServiceControllerStatus.Paused:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Erro ao tentar colocar o serviço {0} para o status {1}.", windowsService.ServiceName, status.ToString()));
                _logger.LogError(string.Format(" - Erro: {0}.", ex));
                return;
            }

            _logger.LogInformation(string.Format("O serviço {0} alterado para o status {1} com sucesso.", windowsService.ServiceName, status.ToString()));
        }

        #endregion

    }
}
