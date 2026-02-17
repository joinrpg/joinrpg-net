using JoinRpg.Common.WebInfrastructure.Cache;
using Microsoft.AspNetCore.Hosting;

namespace JoinRpg.Common.WebInfrastructure;

public static class WebInfrastructureRegistration
{
    public static void AddJoinWebPlatform(this IServiceCollection services,
       IConfiguration configuration, IWebHostEnvironment environment, string appName, string dataProtectionConnectionStringName,
       IEnumerable<string> telemetryServiceNames)
    {
        services.AddJoinDataProtection(configuration, environment, appName, dataProtectionConnectionStringName);
        services.AddJoinOpenTelemetry(appName, telemetryServiceNames);

        if (environment.IsDevelopment())
        {
            services.AddDatabaseDeveloperPageExceptionFilter();
        }

        services.ConfigureForwardedHeaders();

        services
            .AddScoped(typeof(PerRequestCache<,>))
            .AddSingleton(typeof(SingletonCache<,>));
    }
}
