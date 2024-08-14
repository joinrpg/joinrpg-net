using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Extensions.Logging;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace JoinRpg.Portal.Infrastructure.Logging;

public static class SerilogExtensions
{
    public static void ConfigureLogger(this LoggerConfiguration loggerConfiguration, SerilogOptions serilogOptions)
    {
        serilogOptions.LogLevel.TryAdd("Default", LogLevel.Information);
        serilogOptions.LogLevel.TryAdd("Microsoft", LogLevel.Information);
        serilogOptions.LogLevel.TryAdd("Microsoft.AspNetCore", LogLevel.Warning);

        var globalMinimumLogLevelSerilog = LevelConvert.ToSerilogLevel(serilogOptions.LogLevel["Default"]);

        loggerConfiguration
            .MinimumLevel.ControlledBy(new LoggingLevelSwitch(globalMinimumLogLevelSerilog))
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.With<YcLevelEnricher>()
            .Enrich.With<LoggedUserEnricher>()
            .Enrich.WithProperty("AppName", "JoinRpg.Portal");

        foreach (var (@namespace, logLevel) in serilogOptions.LogLevel)
        {
            if (@namespace == "Default")
            {
                continue;
            }
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override(@namespace, LevelConvert.ToSerilogLevel(logLevel));
        }

        if (serilogOptions.Structured)
        {
            var topLevelPropertiesNames = new HashSet<string>()
            {
                "AppName", "TokenId", "UserId", "UserName", "TraceId", "SpanId", "MachineName",
                "Host", "Protocol", "Scheme", "ResponseContentType", "RequestMethod", "RequestPath",
                "StatusCode", "Elapsed", "SourceContext", "RequestId", "ConnectionId", "EndpointName",
                "RouteData", "ActionName", "ActionId", "ValidationState", "RazorPageHandler", "YcLevel",
                "QueryString", "ViewComponentName", "ViewComponentId", "LoggedUser", "ProjectId",
            };

            loggerConfiguration.WriteTo.Console(formatter: new CustomJsonFormatter(topLevelPropertiesNames));
        }
        else
        {
            var template = "[{UtcDateTime(@t):dd-MM-yyyy HH:mm:ss.fff} {#if @l='Verbose'}Trace{#else if @l='Fatal'}Critical{#else}{@l}{#end} ({SourceContext})] {@m}\n{@x}";
            loggerConfiguration.WriteTo.Console(formatter: new ExpressionTemplate(template: template, theme: TemplateTheme.Code));
        }

        if (serilogOptions.SelfLogEnabled)
        {
            SelfLog.Enable(Console.Error);
        }
    }
}
