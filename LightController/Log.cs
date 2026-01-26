using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using System;

namespace LightController;

public static class Log
{
    private static Logger log;

    public static void Init(string file)
    {
        ExpressionTemplate format = new ExpressionTemplate("{@t:HH:mm:ss} [{@l:u3}] [{ThreadID}]{CustomPrefix} {@m} {@x}\n");
        log = new LoggerConfiguration()
            .Enrich.With(new ThreadIDEnricher())
#if DEBUG
            .WriteTo.Debug(format)
#endif
            .WriteTo.Console(format)
            .WriteTo.File(format, file, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += UnhandledException;

        Logger commonLog = new LoggerConfiguration()
            .Enrich.With(new PrefixEnricher("[Bacnet]"))
            .WriteTo.Logger(log) 
            .CreateLogger();

        Common.Logging.LogManager.Adapter = new Common.Logging.Serilog.SerilogFactoryAdapter(commonLog);
    }

    private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
                log.Fatal(ex, "An exception was thrown:");
            else
                log.Fatal("An unhandled exception occurred.");
        }
        catch { } // Unhandled exceptions in the unhandled exception hander are very bad
    }

    public static void Info(string msg)
    {
        Write(LogEventLevel.Information, msg);
    }

    public static void Error(string msg)
    {
        Write(LogEventLevel.Error, msg);
    }

    public static void Warn(string msg)
    {
        Write(LogEventLevel.Warning, msg);
    }

    public static void Error(Exception ex)
    {
        log.Error(ex, "An exception was thrown:");
    }

    public static void Error(Exception ex, string msg)
    {
        log.Error(ex, msg);
    }

    private static void Write(LogEventLevel level, string msg)
    {
        log.Write(level, msg);
    }

    private class PrefixEnricher : ILogEventEnricher
    {
        private readonly string prefix;

        public PrefixEnricher(string prefix)
        {
            this.prefix = " " + prefix;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CustomPrefix", prefix));
        }
    }

    private class ThreadIDEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
              "ThreadID", Environment.CurrentManagedThreadId.ToString()));
        }
    }
}
