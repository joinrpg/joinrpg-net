using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.EntityFrameworkCore;

/// <summary>
/// Регистрация EF Core-контекстов JoinRpg в DI (PostgreSQL).
/// </summary>
public static class DbContextRegisterHelper
{
    /// <summary>
    /// Регистрирует <see cref="DbContext"/> для PostgreSQL: Npgsql-провайдер, EntityFramework.Exceptions
    /// (бизнес-исключения вместо DbUpdateException), лог SQL в Debug в dev-режиме и health check
    /// с именем <c>{connectionStringName}-db</c>. Если строки подключения с таким именем нет,
    /// ничего не регистрирует и возвращает <c>false</c> — вызывающий может решить, критично ли это.
    /// </summary>
    public static bool AddJoinEfCoreDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionStringName,
        Action<DbContextOptionsBuilder>? optionsBuilder = null)
        where TContext : DbContext
        => AddJoinEfCoreDbContext<TContext>(
            services, configuration, environment, connectionStringName, optionsBuilder,
            includeInReadyHealthCheck: true);

    /// <summary>
    /// Регистрирует <see cref="DbContext"/> для PostgreSQL: Npgsql-провайдер, EntityFramework.Exceptions
    /// (бизнес-исключения вместо DbUpdateException), лог SQL в Debug в dev-режиме и health check
    /// с именем <c>{connectionStringName}-db</c>. Если строки подключения с таким именем нет,
    /// ничего не регистрирует и возвращает <c>false</c> — вызывающий может решить, критично ли это.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация со строками подключения.</param>
    /// <param name="environment">Окружение хоста (включает лог SQL в Development).</param>
    /// <param name="connectionStringName">Имя строки подключения.</param>
    /// <param name="optionsBuilder">Дополнительная настройка <see cref="DbContextOptionsBuilder"/>.</param>
    /// <param name="includeInReadyHealthCheck">
    /// Помечать ли health check тегом <c>ready</c>, которым readiness-проба <c>/health/ready</c>
    /// фильтрует чеки, относящиеся к готовности к трафику. Отключайте для баз, которые не нужны
    /// для обработки запросов (очередь уведомлений, расписание джоб), чтобы их чеки не попадали
    /// в readiness-отчёт. Учтите: чек регистрируется со статусом Degraded при недоступности базы,
    /// который по умолчанию не роняет пробу (HTTP 200).
    /// </param>
    public static bool AddJoinEfCoreDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionStringName,
        Action<DbContextOptionsBuilder>? optionsBuilder,
        bool includeInReadyHealthCheck)
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

            optionsBuilder?.Invoke(options);
        });

        services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: $"{connectionStringName}-db",
                failureStatus: HealthStatus.Degraded,
                tags: includeInReadyHealthCheck ? ["ready"] : null);

        return true;
    }
}
