using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebInfrastructure;

public static class DbContextRegisterHelper
{
    public static bool AddJoinEfCoreDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment, string connectionStringName)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        services.AddDbContext<TContext>(
        options =>
        {
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging(environment.IsDevelopment());
            options.EnableDetailedErrors(environment.IsDevelopment());
            options.UseExceptionProcessor();
            options
                .ConfigureWarnings(
                    b => b.Log(
                        (RelationalEventId.CommandExecuted, LogLevel.Debug)));
        });

        services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: $"{connectionStringName}-db",
                failureStatus: HealthStatus.Degraded);

        return true;
    }
}
