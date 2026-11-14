using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JoinRpg.Common.WebInfrastructure.LeaderElection;

/// <summary>
/// Регистрация лидер-выбираемых сервисов.
/// </summary>
public static class LeaderElectedRegistration
{
    /// <summary>
    /// Регистрирует hosted-сервис, который крутит <typeparamref name="TService"/> ровно на одной
    /// реплике из N: реплика-лидер держит PostgreSQL advisory lock в существующей БД Data Protection
    /// (<c>ConnectionStrings:DataProtection</c> по умолчанию; отдельная база и миграции не нужны —
    /// advisory locks не требуют таблиц). Лок живёт на выделенном непулинговом соединении внутри
    /// <c>PostgresLeaderLock</c>: пулинговое соединение EF Core вернулось бы в пул раньше unlock'а.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация со строками подключения.</param>
    /// <param name="lockKey">Ключ advisory lock'а (bigint). Одинаковый у всех реплик одного сервиса,
    /// разный у разных сервисов: локи с разными ключами независимы.</param>
    /// <param name="connectionStringName">Имя строки подключения к PostgreSQL. По умолчанию —
    /// база ключей Data Protection, она есть у всех хостов.</param>
    /// <param name="retryDelay">Пауза между попытками перехватить лок и после сбоев. По умолчанию — 5 секунд.</param>
    public static IServiceCollection AddLeaderElectedHostedService<TService>(
        this IServiceCollection services,
        IConfiguration configuration,
        long lockKey,
        string connectionStringName = "DataProtection",
        TimeSpan? retryDelay = null)
        where TService : class, ILeaderElectedService
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' is required for leader election of {typeof(TService).Name}");

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ILeaderLock>(_ => new PostgresLeaderLock(connectionString));
        services.AddScoped<TService>();
        _ = services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LeaderElectedHostedService<TService>>(
            sp, lockKey, retryDelay ?? TimeSpan.FromSeconds(5)));
        return services;
    }
}
