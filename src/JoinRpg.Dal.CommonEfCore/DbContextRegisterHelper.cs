using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Dal.CommonEfCore;

public static class DbContextRegisterHelper
{
    public static void AddJoinEfCoreDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment, string connectionStringName)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        services.AddDbContext<TContext>(
        options =>
        {
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging(environment.IsDevelopment());
            options.EnableDetailedErrors(environment.IsDevelopment());
        });

        services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: $"{connectionStringName}-db",
                failureStatus: HealthStatus.Degraded);
    }
}
