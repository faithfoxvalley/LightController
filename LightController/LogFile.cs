using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LightController
{
    public static class LogFile
    {
        private static Serilog.Core.Logger log;

        public static void Init(string file)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            log = new LoggerConfiguration()
#if DEBUG
                .WriteTo.Debug()
#endif
                .WriteTo.File(file, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                log.Fatal(ex,
                    "[" + Environment.CurrentManagedThreadId.ToString() + "] An exception was thrown.");
        }

        public static void Info(string msg)
        {
            Write(Serilog.Events.LogEventLevel.Information, msg);
        }

        public static void Error(string msg)
        {
            Write(Serilog.Events.LogEventLevel.Error, msg);
        }

        private static void Write(Serilog.Events.LogEventLevel level, string msg)
        {
            msg = "[" + Environment.CurrentManagedThreadId.ToString() + "] " + msg;
            log.Write(level, msg);
        }
    }
}
