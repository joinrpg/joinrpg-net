using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

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
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация со строками подключения.</param>
    /// <param name="environment">Окружение хоста (включает лог SQL в Development).</param>
    /// <param name="connectionStringName">Имя строки подключения.</param>
    /// <param name="optionsBuilder">Дополнительная настройка <see cref="DbContextOptionsBuilder"/>.</param>
    /// <param name="includeInReadyHealthCheck">
    /// Включать ли health check в readiness-пробу (<c>/health/ready</c> фильтрует чеки по тегу
    /// <c>ready</c>). По умолчанию <c>true</c>: база нужна для обработки запросов. Отключайте
    /// для баз, без которых приложение запускается и работает (очередь уведомлений, расписание
    /// джоб) — иначе недоступность такой базы будет держать под внешним трафиком неготовый под.
    /// </param>
    public static bool AddJoinEfCoreDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionStringName,
        Action<DbContextOptionsBuilder>? optionsBuilder = null,
        bool includeInReadyHealthCheck = true)
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
                failureStatus: HealthStatus.Degraded,
                tags: includeInReadyHealthCheck ? ["ready"] : null);

        return true;
    }
}
