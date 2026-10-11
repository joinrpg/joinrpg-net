using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.Tools.RestoreLostUsers;

/// <summary>
/// Чистая логика отбора: какие пользователи из базы уведомлений потеряны в MSSQL и можно ли
/// восстановить каждого из них. Без обращения к БД — всё нужное передаётся снаружи.
/// </summary>
/// <param name="existingUsers">Все пользователи, которые сейчас есть в MSSQL.</param>
/// <param name="systemUserIds">Виртуальные пользователи (робот, платежи) — их не трогаем.</param>
/// <param name="backupMaxUserId">Максимальный UserId в восстановленном бэкапе. Всё, что выше, — потерянные id.</param>
/// <param name="lostAt">
/// Момент аварии. Уведомления с этого момента не учитываются: если сайт открыли до восстановления, новый
/// регистрант мог занять потерянный id, и его уведомления выдавали бы его адрес за адрес потерянного пользователя.
/// Все запросы к базе уведомлений уже фильтруют по этому моменту. Для адресов почты отсечка повторена
/// здесь: от неё зависит главный конфликт (id занят новым регистрантом), и она должна быть видна в тестах.
/// </param>
internal class LostUserPlanner(IReadOnlyCollection<ExistingUser> existingUsers, IReadOnlySet<int> systemUserIds, int backupMaxUserId, DateTimeOffset lostAt)
{
    private readonly Dictionary<int, ExistingUser> existingById = existingUsers.ToDictionary(u => u.UserId);

    // В MSSQL колляция без учёта регистра, вход ищет пользователя по Email, а UserName совпадает с Email.
    // Поэтому адрес считаем занятым, если он есть в любой из двух колонок, без учёта регистра.
    private readonly Dictionary<string, int> existingByEmail = BuildEmailIndex(existingUsers);

    private static Dictionary<string, int> BuildEmailIndex(IReadOnlyCollection<ExistingUser> users)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in users.OrderBy(u => u.UserId))
        {
            foreach (var value in new[] { user.Email, user.UserName })
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _ = result.TryAdd(value.Trim(), user.UserId);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// По каким id нужны подробности из базы уведомлений: всех, кого нет в MSSQL, и тех, кто есть,
    /// но с id выше максимума из бэкапа (для проверки, не занят ли id кем-то другим).
    /// </summary>
    public IReadOnlyCollection<int> SelectIdsNeedingDetails(IEnumerable<NotificationUserActivity> activity)
        => [.. activity.Select(a => a.UserId).Where(NeedsDecision).Distinct().Order()];

    private bool NeedsDecision(int userId)
        => !systemUserIds.Contains(userId) && (!existingById.ContainsKey(userId) || userId > backupMaxUserId);

    public IReadOnlyList<LostUserDecision> Plan(
        IEnumerable<NotificationUserActivity> activity,
        IReadOnlyDictionary<int, NotificationUserDetails> details)
    {
        var decisions = activity
            .Where(a => NeedsDecision(a.UserId))
            .Select(a => Decide(a, details.GetValueOrDefault(a.UserId)))
            .ToList();

        MarkDuplicateEmails(decisions);

        return [.. decisions.OrderBy(d => d.UserId)];
    }

    private LostUserDecision Decide(NotificationUserActivity activity, NotificationUserDetails? details)
    {
        var emailValues = (details?.EmailValues ?? [])
            .Where(v => v.LastSentAt < lostAt)
            .OrderByDescending(v => v.LastSentAt)
            .ToList();
        var emailValue = emailValues.FirstOrDefault()?.Value;
        var email = emailValue is null ? null : TryParseEmail(emailValue);
        var allEmails = emailValues
            .Select(v => TryParseEmail(v.Value) ?? v.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var (telegramChatId, telegramUserName) = ParseTelegram(details?.LatestTelegramValue);
        var name = details is null ? null : GreetingNameParser.ChooseName(details.Greetings, activity.UserId, email);

        LostUserDecision Make(DecisionKind kind, int? conflictingUserId = null) => new(
            activity.UserId,
            kind,
            email ?? emailValue,
            name,
            telegramChatId,
            telegramUserName,
            activity.FirstSeen,
            activity.LastSeen,
            activity.AsRecipient,
            activity.AsInitiator,
            conflictingUserId,
            allEmails.Count > 1 ? allEmails : null);

        if (activity.UserId <= 0)
        {
            return Make(DecisionKind.InvalidId);
        }

        if (existingById.TryGetValue(activity.UserId, out var existing))
        {
            // Сюда попадают только id выше максимума из бэкапа. Сами мы создаём пользователя только с адресом
            // из уведомлений до аварии, так что «восстановлен ранее» — это ровно совпадение адресов.
            var existingEmail = existing.Email?.Trim();
            return email is not null && string.Equals(email, existingEmail, StringComparison.OrdinalIgnoreCase)
                ? Make(DecisionKind.AlreadyExists)
                : Make(DecisionKind.IdTakenByOtherUser, existing.UserId);
        }

        if (activity.UserId <= backupMaxUserId)
        {
            return Make(DecisionKind.IdNotAboveBackupMax);
        }

        if (emailValue is null)
        {
            return Make(DecisionKind.NoEmail);
        }

        if (email is null)
        {
            return Make(DecisionKind.InvalidEmail);
        }

        if (existingByEmail.TryGetValue(email, out var ownerId))
        {
            return Make(DecisionKind.EmailTaken, ownerId);
        }

        return Make(DecisionKind.Create);
    }

    private static string? TryParseEmail(string channelSpecificValue)
        => Email.TryParse(channelSpecificValue, null, out var email) && !string.IsNullOrWhiteSpace(email.Value)
            ? email.Value.Trim()
            : null;

    /// <summary>
    /// С #4642 (2026-08-26) канал Telegram пишет TelegramChatId(123), до того — Telegram(123, @name).
    /// </summary>
    internal static (long? ChatId, string? UserName) ParseTelegram(string? channelSpecificValue)
    {
        if (channelSpecificValue is null)
        {
            return (null, null);
        }
        if (TelegramChatId.TryParse(channelSpecificValue, null, out var chatId))
        {
            return (chatId.Value, null);
        }
        if (TelegramSocialLink.TryParse(channelSpecificValue, null, out var link))
        {
            return (link.ChatId?.Value, link.PrettyName?.Value);
        }
        return (null, null);
    }

    private static void MarkDuplicateEmails(List<LostUserDecision> decisions)
    {
        var duplicateGroups = decisions
            .Select((d, index) => (d, index))
            .Where(x => x.d.Kind == DecisionKind.Create)
            .GroupBy(x => x.d.Email!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            var members = group.ToList();
            foreach (var (decision, index) in members)
            {
                var other = members.First(m => m.d.UserId != decision.UserId).d.UserId;
                decisions[index] = decision with { Kind = DecisionKind.DuplicateEmail, ConflictingUserId = other };
            }
        }
    }
}
