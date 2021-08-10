using Hangfire.Job.Core;
using Hangfire.Job.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Job.HelloWorld
{
    public class BootStrapper : HangfireJob
    {
        public BootStrapper(string jobName, LoggerEventLevel logEventLevel)
            : base(jobName, logEventLevel)
        {
        }

        public override void Execute()
        {
            Logger.WriteLog.Information("Hello World!");
            Thread.Sleep(1000 * 30);  // 600,000 ms = 600 sec = 10 min
            Logger.WriteLog.Information("Bye World!");
        }

        public override void Dispose()
        {
            this.Dispose();
        }
    }
}
