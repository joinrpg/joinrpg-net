using JoinRpg.Common.WebInfrastructure.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace JoinRpg.Common.WebInfrastructure;
public static class LoggingExtensions
{
    public static IApplicationBuilder UseJoinRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(opts =>
        {
            opts.EnrichDiagnosticContext = SerilogWebRequestHelper.EnrichFromRequest;
            opts.GetLevel = SerilogWebRequestHelper.ExcludeHealthChecks;
        });
    }

    public static IHostBuilder UseJoinSerilog(this IHostBuilder host, string appname)
    {
        return host.UseSerilog((context, _, configuration) =>
        {
            var loggerOptions = context.Configuration.GetSection("Logging").Get<SerilogOptions>();

            configuration.ConfigureLogger(loggerOptions!, appname);
        });
    }

}
