using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.Portal.Infrastructure;

public static class DbContextRegisterHelper
{
    public static void AddJoinEfCoreDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment, string connectionStringName)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<TContext>(
            options =>
            {
                options.UseNpgsql(connectionString);
                options.EnableSensitiveDataLogging(environment.IsDevelopment());
                options.EnableDetailedErrors(environment.IsDevelopment());
            });

            services.AddDatabaseDeveloperPageExceptionFilter();
            services
                .AddHealthChecks()
                .AddNpgSql(
                    connectionString,
                    name: $"{connectionStringName}-db",
                    failureStatus: HealthStatus.Degraded);
        }
    }
}
