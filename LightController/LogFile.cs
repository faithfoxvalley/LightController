using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController
{
    public static class LogFile
    {
        private static Serilog.Core.Logger log;

        public static void Init(string file)
        {
            log = new LoggerConfiguration()
#if DEBUG
                .WriteTo.Debug()
#endif
                .WriteTo.File(file, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public static void Info(string msg)
        {
            msg = "[" + Environment.CurrentManagedThreadId.ToString() + "] " + msg;
            log.Write(Serilog.Events.LogEventLevel.Information, msg);
        }
    }
}
