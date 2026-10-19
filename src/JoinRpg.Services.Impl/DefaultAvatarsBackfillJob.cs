using JoinRpg.Services.Interfaces.Avatars;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Бэкфилл аватарок (#5602): заводит Gravatar всем, у кого нет выбранной аватарки. Это пользователи старше
/// миграции SaveAvatar (2021) и заготовки, восстановленные после потери базы (DRP #5459). Новые пользователи
/// получают аватарку при создании, так что после прогона на чтении её заводить больше не придётся.
/// Идемпотентна: повторный прогон не находит пользователей без аватарки. Временная — удаляется после отработки на проде.
/// </summary>
[Obsolete("Разовая починка данных под #5602, удалить после отработки на проде")]
internal class DefaultAvatarsBackfillJob(
    IUserRepository userRepository,
    IServiceScopeFactory scopeFactory,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<DefaultAvatarsBackfillJob> logger
    ) : IDailyJob
{
    private const int BatchSize = 500;

    public async Task RunOnce(CancellationToken cancellationToken)
    {
        UserIdentification? after = null;
        var processed = 0;
        var failed = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Идём по возрастанию id: каждая пачка выбирается один раз, даже если сохранить её не удалось,
            // поэтому цикл гарантированно заканчивается.
            var batch = await userRepository.GetUsersWithoutSelectedAvatar(new KeySetPagination(after?.Value, BatchSize));
            if (batch.Count == 0)
            {
                break;
            }
            after = batch.Last();

            // Свой scope, а с ним свой DbContext, на каждую пачку: трекер не копит десятки тысяч пользователей,
            // а сущности упавшего SaveChanges не валят следующие пачки.
            // Имперсонация робота (её ставит JobRunner) живёт в scope — в новом её надо повторить.
            using var scope = scopeFactory.CreateScope();
            var impersonate = scope.ServiceProvider.GetRequiredService<IImpersonateAccessor>();
            impersonate.StartImpersonate(currentUserAccessor.UserIdentification, currentUserAccessor.DisplayName, currentUserAccessor.IsAdmin);
            try
            {
                await scope.ServiceProvider.GetRequiredService<IAvatarService>().EnsureDefaultAvatars(batch);
                processed += batch.Count;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Пачку пропускаем до следующего прогона.
                logger.LogError(ex, "Не удалось завести аватарки пачке пользователей с {firstUserId} по {lastUserId}", batch.First(), batch.Last());
                failed += batch.Count;
            }
            finally
            {
                impersonate.StopImpersonate();
            }
        }

        logger.LogInformation("Аватарки по умолчанию заведены {processedCount} пользователям, не удалось — {failedCount}", processed, failed);
    }
}
