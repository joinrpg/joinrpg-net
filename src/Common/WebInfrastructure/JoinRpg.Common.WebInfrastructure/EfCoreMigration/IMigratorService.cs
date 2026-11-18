namespace JoinRpg.Common.WebInfrastructure.EfCoreMigration;

public interface IMigratorService
{
    /// <summary>
    /// Имя для логов: по нему должно быть понятно, какая именно база.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Проверить, что до базы можно достучаться, ничего в ней не меняя. Лаунчер зовёт проверку у всех
    /// миграторов до первой миграции, чтобы кривая строка подключения к одной базе не оставила
    /// остальные накаченными наполовину.
    /// </summary>
    Task CheckConnectionAsync(CancellationToken ct);

    Task MigrateAsync(CancellationToken ct);
}
