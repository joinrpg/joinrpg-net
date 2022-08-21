using Serilog.Core;
using Serilog.Events;

namespace JoinRpg.Portal.Infrastructure.Logging;

public class YcLevelEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("YcLevel", YcLogLevelConverter(logEvent.Level)));
    }

    //https://cloud.yandex.ru/docs/logging/concepts/filter#parameters -> level
    private static string YcLogLevelConverter(LogEventLevel level) =>
        level switch
        {
            LogEventLevel.Verbose => "TRACE",
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARN",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "FATAL",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, $@"Unknown {nameof(LogEventLevel)} kind: {level}")
        };
}
