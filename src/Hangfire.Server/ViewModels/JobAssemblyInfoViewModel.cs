using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.Server.ViewModels
{
    public class JobAssemblyInfoViewModel
    {
        public string JobName { get; set; }
        public string LogLevel { get; set; }
        public string DllJob { get; set; }
        public string Cron { get; set; }
        public string Queue { get; set; }
        public string[] LogMetod { get; set; }
        public bool RecurringJob { get; set; }

    }
}
