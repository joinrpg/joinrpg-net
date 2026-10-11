using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Перенос ссылок SQL-апдейтами, а не загрузкой сущностей: у активного пользователя тысячи
/// комментариев, и через EF6 это ленивые загрузки и минуты работы (ADR024).
/// </summary>
internal class UserMergeWriteRepository(MyDbContext ctx) : IUserMergeWriteRepository
{
    async Task<IReadOnlyDictionary<string, int>> IUserMergeWriteRepository.MergeAndDelete(
        UserIdentification primary, UserIdentification secondary, Action mergeProfile)
    {
        int a = primary.Value, b = secondary.Value;
        var affected = new Dictionary<string, int>();

        using var transaction = ctx.Database.BeginTransaction();

        // Между предпросмотром и подтверждением данные могли измениться — проверяем ещё раз, уже в транзакции.
        var preview = await ((IUserMergeRepository)new UserMergeRepository(ctx)).GetPreview(primary, secondary);
        if (!preview.CanMerge)
        {
            throw new UserMergeConflictException(preview);
        }

        // Профиль правится на трекаемых сущностях только после проверки: при отказе в контексте
        // не должно остаться наполовину слитого профиля, который сохранит чужой SaveChanges.
        mergeProfile();
        _ = await ctx.SaveChangesAsync();

        async Task Execute(UserReference reference, string sql, string? suffix = null)
        {
            var key = suffix is null ? reference.ToString() : $"{reference} ({suffix})";
            affected[key] = affected.GetValueOrDefault(key) + await ctx.Database.ExecuteSqlCommandAsync(sql, a, b);
        }

        foreach (var reference in UserReferences.Reassigned)
        {
            await Execute(reference, $"UPDATE [{reference.Table}] SET [{reference.Column}] = @p0 WHERE [{reference.Column}] = @p1");
        }

        // UK (UserId, Provider): привязка основного побеждает, такая же у поглощаемого удаляется.
        await Execute(UserReferences.ExternalLogins, """
            DELETE b FROM [UserExternalLogins] b
            WHERE b.[UserId] = @p1
              AND EXISTS (SELECT 1 FROM [UserExternalLogins] a
                          WHERE a.[UserId] = @p0
                            AND (a.[Provider] = b.[Provider] OR (a.[Provider] IS NULL AND b.[Provider] IS NULL)))
            """, "дубли удалены");
        await Execute(UserReferences.ExternalLogins, "UPDATE [UserExternalLogins] SET [UserId] = @p0 WHERE [UserId] = @p1");

        // Дубль подписки на то же самое ломает SingleOrDefault в коде подписок: оставляем подписку
        // основного, объединив флаги — уведомления, на которые был подписан поглощаемый, не пропадут.
        const string sameSubscription = """
            a.[UserId] = @p0 AND b.[UserId] = @p1
            AND a.[ProjectId] = b.[ProjectId]
            AND (a.[CharacterGroupId] = b.[CharacterGroupId] OR (a.[CharacterGroupId] IS NULL AND b.[CharacterGroupId] IS NULL))
            AND (a.[CharacterId] = b.[CharacterId] OR (a.[CharacterId] IS NULL AND b.[CharacterId] IS NULL))
            AND (a.[ClaimId] = b.[ClaimId] OR (a.[ClaimId] IS NULL AND b.[ClaimId] IS NULL))
            """;
        await Execute(UserReferences.Subscriptions, $"""
            UPDATE a SET
                a.[ClaimStatusChange] = a.[ClaimStatusChange] | b.[ClaimStatusChange],
                a.[Comments] = a.[Comments] | b.[Comments],
                a.[FieldChange] = a.[FieldChange] | b.[FieldChange],
                a.[MoneyOperation] = a.[MoneyOperation] | b.[MoneyOperation],
                a.[AccommodationChange] = a.[AccommodationChange] | b.[AccommodationChange]
            FROM [UserSubscriptions] a JOIN [UserSubscriptions] b ON {sameSubscription}
            """, "флаги объединены");
        await Execute(UserReferences.Subscriptions, $"""
            DELETE b FROM [UserSubscriptions] b
            WHERE EXISTS (SELECT 1 FROM [UserSubscriptions] a WHERE {sameSubscription})
            """, "дубли удалены");
        await Execute(UserReferences.Subscriptions, "UPDATE [UserSubscriptions] SET [UserId] = @p0 WHERE [UserId] = @p1");

        await Execute(UserReferences.ForumSubscriptions, """
            DELETE b FROM [UserForumSubscriptions] b
            WHERE b.[UserId] = @p1
              AND EXISTS (SELECT 1 FROM [UserForumSubscriptions] a WHERE a.[UserId] = @p0 AND a.[ForumThreadId] = b.[ForumThreadId])
            """, "дубли удалены");
        await Execute(UserReferences.ForumSubscriptions, "UPDATE [UserForumSubscriptions] SET [UserId] = @p0 WHERE [UserId] = @p1");

        // Аватары уже переехали к основному; если своей выбранной у него нет — берём выбранную поглощаемого.
        _ = await ctx.Database.ExecuteSqlCommandAsync("""
            UPDATE a SET a.[SelectedAvatarId] = b.[SelectedAvatarId]
            FROM [Users] a CROSS JOIN [Users] b
            WHERE a.[UserId] = @p0 AND b.[UserId] = @p1 AND a.[SelectedAvatarId] IS NULL
            """, a, b);

        // Поля 1:1 уже перенесены в профиль основного на сущностях, строки поглощаемого больше не нужны.
        foreach (var reference in UserReferences.OneToOne)
        {
            await Execute(reference, $"DELETE FROM [{reference.Table}] WHERE [{reference.Column}] = @p1");
        }

        var deletedUsers = await ctx.Database.ExecuteSqlCommandAsync("DELETE FROM [Users] WHERE [UserId] = @p1", a, b);
        if (deletedUsers != 1)
        {
            throw new InvalidOperationException($"Пользователь {secondary} не удалён при слиянии в {primary}");
        }

        transaction.Commit();
        return affected;
    }
}

/// <summary>Колонка, которая ссылается на <c>Users.UserId</c>.</summary>
public record UserReference(string Table, string Column)
{
    public override string ToString() => $"{Table}.{Column}";
}

/// <summary>
/// Все ссылки на пользователя в основной БД и то, как с ними поступает слияние (ADR024).
/// Полноту списка сверяет с реальной схемой интеграционный тест: новый FK на <c>Users</c> без
/// правила слияния — красный тест, а не отказ <c>DELETE</c> (или тихий каскад) в проде.
/// </summary>
public static class UserReferences
{
    /// <summary>Ссылка с поглощаемого просто переписывается на основного.</summary>
    public static readonly UserReference[] Reassigned =
    [
        new("CaptainAccessRuleEntities", "CaptainUserId"),
        new("CharacterGroups", "CreatedById"),
        new("CharacterGroups", "ResponsibleMasterUserId"),
        new("CharacterGroups", "UpdatedById"),
        new("Characters", "CreatedById"),
        new("Characters", "UpdatedById"),
        new("Claims", "LastMasterCommentBy_Id"),
        new("Claims", "LastVisibleMasterCommentBy_Id"),
        new("Claims", "PlayerUserId"),
        new("Claims", "ResponsibleMasterUserId"),
        new("Comments", "AuthorUserId"),
        new("ForumThreads", "AuthorUserId"),
        new("GameReport2DTemplate", "CreatedById"),
        new("GameReport2DTemplate", "UpdatedById"),
        new("MoneyTransfers", "ChangedById"),
        new("MoneyTransfers", "CreatedById"),
        new("MoneyTransfers", "ReceiverId"),
        new("MoneyTransfers", "SenderId"),
        new("PaymentTypes", "UserId"),
        new("PlotElementTexts", "AuthorUserId"),
        new("ProjectAcls", "UserId"),
        // Дубли отметок «прочитано» по одному обсуждению код терпит и сам подчищает.
        new("ReadCommentWatermarks", "UserId"),
        new("UserAvatars", "UserId"),
    ];

    /// <summary>Переписываются, но дубли с основным у поглощаемого сначала удаляются.</summary>
    public static readonly UserReference ExternalLogins = new("UserExternalLogins", "UserId");

    /// <inheritdoc cref="ExternalLogins"/>
    public static readonly UserReference Subscriptions = new("UserSubscriptions", "UserId");

    /// <inheritdoc cref="ExternalLogins"/>
    public static readonly UserReference ForumSubscriptions = new("UserForumSubscriptions", "UserId");

    /// <summary>Строки 1:1 с PK = UserId: поля сливаются на сущностях, строки поглощаемого удаляются.</summary>
    public static readonly UserReference[] OneToOne =
    [
        new("AllrpgUserDetails", "UserId"),
        new("UserAuthDetails", "UserId"),
        new("UserExtras", "UserId"),
    ];

    public static IEnumerable<UserReference> All =>
        [.. Reassigned, ExternalLogins, Subscriptions, ForumSubscriptions, .. OneToOne];
}
