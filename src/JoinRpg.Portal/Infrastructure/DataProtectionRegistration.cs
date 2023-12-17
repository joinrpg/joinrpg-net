using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.Portal.Infrastructure;

public static class DataProtectionRegistration
{
    public static void AddJoinDataProtection(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection();
        var dataProtectionConnectionString = configuration.GetConnectionString("DataProtection");

        if (!environment.IsDevelopment() && !string.IsNullOrWhiteSpace(dataProtectionConnectionString))
        {
            services.AddDbContext<DataProtectionDbContext>(
            options =>
            {
                options.UseNpgsql(dataProtectionConnectionString);
                options.EnableSensitiveDataLogging(environment.IsDevelopment());
                options.EnableDetailedErrors(environment.IsDevelopment());
            });

            services.AddDatabaseDeveloperPageExceptionFilter();
            services
                .AddHealthChecks()
                .AddNpgSql(
                    dataProtectionConnectionString,
                    name: "dataprotection-db",
                    failureStatus: HealthStatus.Degraded);
            dataProtection.PersistKeysToDbContext<DataProtectionDbContext>();
        }
    }
}
