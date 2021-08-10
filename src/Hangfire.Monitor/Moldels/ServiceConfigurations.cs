using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Monitor.Moldels
{
    public class ServiceConfigurations
    {
        public WindowsServiceConfiguration[] Services { get; set; }
        public int Intervalo { get; set; }

    }

    public class WindowsServiceConfiguration
    {
        public string Nome { get; set; }
        public string Estado { get; set; }
    }
}
