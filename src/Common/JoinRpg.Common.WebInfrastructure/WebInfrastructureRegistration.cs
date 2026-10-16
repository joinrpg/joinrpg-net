using JoinRpg.Common.WebInfrastructure.Cache;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.Common.WebInfrastructure;

public static class WebInfrastructureRegistration
{
    public static void AddJoinWebPlatform(this IServiceCollection services,
       IConfiguration configuration, IWebHostEnvironment environment, string appName, string dataProtectionConnectionStringName,
       IEnumerable<string> telemetryServiceNames)
    {
        services.AddJoinDataProtection(configuration, environment, appName, dataProtectionConnectionStringName);
        services.AddJoinWebPlatformCore(environment, appName, telemetryServiceNames);
    }

    /// <summary>
    /// То же, но ключи Data Protection хранятся в DbContext приложения <typeparamref name="TKeysContext"/>,
    /// а не в отдельной базе. Контекст регистрируется отдельно.
    /// </summary>
    public static void AddJoinWebPlatform<TKeysContext>(this IServiceCollection services,
       IWebHostEnvironment environment, string appName, IEnumerable<string> telemetryServiceNames)
        where TKeysContext : DbContext, IDataProtectionKeyContext
    {
        services.AddJoinDataProtection<TKeysContext>(appName);
        services.AddJoinWebPlatformCore(environment, appName, telemetryServiceNames);
    }

    private static void AddJoinWebPlatformCore(this IServiceCollection services,
       IWebHostEnvironment environment, string appName, IEnumerable<string> telemetryServiceNames)
    {
        services.AddJoinOpenTelemetry(appName, telemetryServiceNames);

        if (environment.IsDevelopment())
        {
            services.AddDatabaseDeveloperPageExceptionFilter();
        }

        services.ConfigureForwardedHeaders();

        services.AddJoinHealthChecks();

        services.AddOptions<SiteBannerOptions>().BindConfiguration(SiteBannerOptions.SectionName);

        services
            .AddScoped(typeof(PerRequestCache<,>))
            .AddSingleton(typeof(SingletonCache<,>))
            .AddSingleton<IHealthCheckPublisher, HealthCheckMetricsPublisher>();
    }
}
