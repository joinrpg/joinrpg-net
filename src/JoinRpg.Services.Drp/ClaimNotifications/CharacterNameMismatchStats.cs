using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Drp.ClaimNotifications;

/// <summary>
/// В скольких принятых заявках имя персонажа по уведомлениям отличается от имени игрока.
/// </summary>
/// <remarks>
/// Без поля-имени имя персонажа при принятии заявки берётся из имени игрока, и в уведомлении о принятии они
/// совпадают. Если до аварии они заметно расходились, поле-имя в проекте было — а в восстановленном проекте
/// его забыли выбрать, и восстановленные персонажи получат имена игроков.
/// Смотрим только уведомления о принятии и о второй роли: только там имя персонажа заново вычислено из полей
/// (<c>ClaimServiceImpl.ApproveCore</c>, <c>MoveToSecondRole</c>). До принятия персонаж (и шаблон) носит своё имя,
/// а после — имя может устареть: игрок сменил отображаемое имя, мастер перенёс заявку на другого персонажа.
/// </remarks>
/// <param name="ClaimCount">Заявок с уведомлением о принятии, где есть и имя персонажа, и имя игрока.</param>
/// <param name="MismatchedCount">Из них — с разными именами (по последнему такому уведомлению).</param>
public record CharacterNameMismatchStats(int ClaimCount, int MismatchedCount)
{
    /// <summary>Расхождений больше 10% — похоже, имя персонажа бралось из поля.</summary>
    public bool IsSuspicious => MismatchedCount * 10 > ClaimCount;

    public static CharacterNameMismatchStats FromEvents(IEnumerable<ClaimNotificationEvent> events)
    {
        var lastApprovals = events
            .Where(e => e.Action is CommentExtraAction.ApproveByMaster or CommentExtraAction.SecondRole
                && !string.IsNullOrWhiteSpace(e.CharacterName)
                && !string.IsNullOrWhiteSpace(e.PlayerName))
            .GroupBy(e => e.ClaimId)
            .Select(g => g.MaxBy(e => e.CreatedAt)!)
            .ToList();

        var mismatched = lastApprovals.Count(e =>
            !string.Equals(e.CharacterName!.Trim(), e.PlayerName!.Trim(), StringComparison.OrdinalIgnoreCase));

        return new CharacterNameMismatchStats(lastApprovals.Count, mismatched);
    }
}
