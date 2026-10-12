using JoinRpg.Data.Write.Interfaces;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Перевод закешированных аватарок на хранение ключа (#5526): раньше в UserAvatars.CachedUri писался полный адрес
/// с доменом API S3, теперь — только ключ объекта, а домен добавляется из настроек при чтении.
/// Ключ — последний сегмент пути старого адреса. Идемпотентна: повторный прогон не находит абсолютных адресов.
/// Временная — удаляется после отработки на проде.
/// </summary>
[Obsolete("Временный хак")]
internal class AvatarCachedUriToKeyJob(IUnitOfWork unitOfWork, ILogger<AvatarCachedUriToKeyJob> logger) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var updated = await unitOfWork.ExecuteSqlCommandAsync(
            """
            UPDATE dbo.UserAvatars
            SET CachedUri = RIGHT(CachedUri, CHARINDEX('/', REVERSE(CachedUri)) - 1)
            WHERE (CachedUri LIKE 'http://%' OR CachedUri LIKE 'https://%') AND CachedUri NOT LIKE '%/'
            """);

        logger.LogInformation("Адрес аватарки заменён на ключ у {Count} записей", updated);
    }
}
