using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace JoinRpg.Common.WebInfrastructure;

public static class DbContextRegisterHelper
{
    public static bool AddJoinEfCoreDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionStringName,
        Action<DbContextOptionsBuilder>? optionsBuilder = null)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        // Мы не используем Kerberos-аутентификацию к Postgres, а krb5-библиотек в контейнере нет
        // (см. https://github.com/npgsql/npgsql/issues/6360). Без явного отключения Npgsql 10
        // всё равно пытается их найти при каждом подключении и пишет в лог не влияющую ни на что ошибку.
        // В .NET 11 сам рантайм перестанет логировать эту ошибку (https://github.com/dotnet/runtime/issues/126251),
        // но это отключение можно будет убрать не раньше перехода проекта на .NET 11.
        connectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            GssEncryptionMode = GssEncryptionMode.Disable,
        }.ConnectionString;

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

            optionsBuilder?.Invoke(options);
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
