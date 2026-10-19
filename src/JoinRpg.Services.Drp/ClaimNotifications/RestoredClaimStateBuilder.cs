using System.Globalization;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Drp.ClaimNotifications;

/// <summary>
/// Сворачивает события одной заявки (<see cref="ClaimNotificationParser"/>) в её состояние на момент аварии.
/// </summary>
public static class RestoredClaimStateBuilder
{
    private const string ResponsibleArrow = " → ";

    /// <param name="events">
    /// События одной заявки — только после бэкапа: статус переигрывается поверх
    /// <paramref name="knownStatus"/>, и события до бэкапа в нём уже учтены.
    /// </param>
    /// <param name="knownStatus">
    /// Статус в восстановленной базе, если заявка там есть. Нужен, чтобы повторить переход в «обсуждается»
    /// по комментариям: он зависит от того, из какого статуса.
    /// </param>
    /// <param name="knownPlayer">Игрок в восстановленной базе, если заявка там есть.</param>
    public static RestoredClaimState Build(
        ClaimIdentification claimId,
        IReadOnlyCollection<ClaimNotificationEvent> events,
        ClaimStatus? knownStatus = null,
        UserIdentification? knownPlayer = null)
    {
        if (events.Count == 0)
        {
            throw new ArgumentException("Нужно хотя бы одно событие", nameof(events));
        }
        if (events.Any(e => e.ClaimId != claimId))
        {
            throw new ArgumentException("События другой заявки", nameof(events));
        }

        var ordered = events.OrderBy(e => e.CreatedAt).ThenBy(e => e.CommentId).ToList();
        var changes = ordered.Where(e => e.Kind == ClaimNotificationKind.ClaimChange).ToList();

        var (player, candidates, warnings) = FindPlayer(changes);

        ClaimStatus? status = knownStatus;
        DateTimeOffset? statusChangedAt = null;
        var playerForVisibility = knownPlayer ?? player;
        foreach (var e in changes)
        {
            var next = NextStatus(status, e, playerForVisibility);
            if (next != status)
            {
                status = next;
                statusChangedAt = e.CreatedAt;
            }
        }

        var lastResponsibleChange = changes.LastOrDefault(e => e.Action == CommentExtraAction.ChangeResponsible);

        // Вторая роль выпускается с нулевым взносом без отдельного события о нём.
        var lastFee = changes
            .Select(e => e.Action switch
            {
                CommentExtraAction.FeeChanged => ParseFee(e.Text),
                CommentExtraAction.SecondRole => 0,
                _ => null,
            })
            .LastOrDefault(fee => fee is not null);

        return new RestoredClaimState(
            claimId,
            CharacterName: changes.LastOrDefault()?.CharacterName,
            MayHaveChangedCharacter: changes.Any(e => e.Action is CommentExtraAction.MoveByMaster or CommentExtraAction.RestoreByMaster or CommentExtraAction.ApproveByMaster),
            player,
            candidates,
            IsNewClaim: changes.Any(e => e.Action is CommentExtraAction.NewClaim or CommentExtraAction.SecondRole),
            status,
            statusChangedAt,
            ResponsibleMasterName: lastResponsibleChange is null ? null : ParseNewResponsible(lastResponsibleChange.Text),
            ResponsibleChangedAt: lastResponsibleChange?.CreatedAt,
            ManualFee: lastFee,
            FirstActivity: ordered[0].CreatedAt,
            LastActivity: ordered[^1].CreatedAt,
            warnings);
    }

    /// <summary>
    /// Игрок — получатель, которого поприветствовали именем игрока из заголовка. Сам игрок не получает
    /// уведомлений о своих действиях, поэтому запасной вариант — инициатор действий «игроком».
    /// </summary>
    private static (UserIdentification? Player, IReadOnlyCollection<UserIdentification> Candidates, IReadOnlyCollection<RestoredClaimWarning> Warnings)
        FindPlayer(IReadOnlyCollection<ClaimNotificationEvent> changes)
    {
        IReadOnlyCollection<UserIdentification> candidates = [.. changes
            .SelectMany(e => e.Recipients.Where(r => r.GreetingName is not null && r.GreetingName == e.PlayerName))
            .Select(r => r.UserId)
            .Distinct()];

        if (candidates.Count == 0)
        {
            // Запрос льготного взноса всегда подписан «игроком», даже когда его делает мастер.
            candidates = [.. changes
                .Where(e => e.Author == ClaimEventAuthor.Player && e.Action != CommentExtraAction.RequestPreferential)
                .Select(e => e.Initiator)
                .Distinct()];
        }

        return candidates.Count switch
        {
            0 => (null, candidates, [RestoredClaimWarning.PlayerNotFound]),
            1 => (candidates.Single(), candidates, []),
            _ => (null, candidates, [RestoredClaimWarning.PlayerAmbiguous]),
        };
    }

    /// <summary>
    /// До #4518 (2026-08-03) восстановление заявки мастером ставило «подана» и сразу «обсуждается»,
    /// после — «предложена».
    /// </summary>
    internal static readonly DateTimeOffset RestoreByMasterInvitesSince = new(2026, 8, 3, 8, 45, 21, TimeSpan.Zero);

    /// <summary>
    /// До #5123 (2026-09-30) модерация финансовой операции шла через обычный комментарий и, как он,
    /// выводила заявку в «обсуждается»; модерация всегда видна игроку.
    /// </summary>
    internal static readonly DateTimeOffset FinanceModerationKeepsStatusSince = new(2026, 9, 30, 20, 54, 11, TimeSpan.Zero);

    /// <summary>
    /// Переходы — как в <c>ClaimServiceImpl</c> на момент события: статус ставит действие, а комментарий
    /// противоположной стороны выводит из «подана»/«приглашена» в «обсуждается»
    /// (<c>ClaimStatusTransition.MarkDiscussed</c>).
    /// </summary>
    /// <remarks>
    /// Видимость комментария мастера определяется по тому, получил ли его игрок. Скрытый ответ на
    /// комментарий игрока ему тоже приходит (подписка «ответ на ваш комментарий») — такой случай по
    /// уведомлениям неотличим и даст «обсуждается» там, где заявка осталась «подана».
    /// </remarks>
    internal static ClaimStatus? NextStatus(ClaimStatus? status, ClaimNotificationEvent e, UserIdentification? player)
    {
        var masterVisibleToPlayer = e.Author == ClaimEventAuthor.Master
            && player is not null && e.Recipients.Any(r => r.UserId == player);

        return e.Action switch
        {
            CommentExtraAction.NewClaim => e.Author == ClaimEventAuthor.Player ? ClaimStatus.AddedByUser : ClaimStatus.AddedByMaster,
            CommentExtraAction.ApproveByMaster => ClaimStatus.Approved,
            CommentExtraAction.SecondRole => ClaimStatus.Approved,
            CommentExtraAction.OutOfGame => ClaimStatus.Approved,
            CommentExtraAction.DeclineByMaster => ClaimStatus.DeclinedByMaster,
            CommentExtraAction.DeclineByPlayer => ClaimStatus.DeclinedByUser,
            CommentExtraAction.RestoreByMaster when e.CreatedAt < RestoreByMasterInvitesSince => ClaimStatus.Discussed,
            CommentExtraAction.RestoreByMaster => ClaimStatus.AddedByMaster,
            CommentExtraAction.OnHoldByMaster => ClaimStatus.OnHold,
            CommentExtraAction.CheckedIn => ClaimStatus.CheckedIn,
            // Принять приглашение можно только из «предложена».
            CommentExtraAction.InvitationAcceptedByPlayer => ClaimStatus.Discussed,
            CommentExtraAction.ApproveFinance or CommentExtraAction.RejectFinance
                when e.CreatedAt < FinanceModerationKeepsStatusSince && status is ClaimStatus.AddedByUser && e.Author == ClaimEventAuthor.Master
                => ClaimStatus.Discussed,
            null when status is ClaimStatus.AddedByMaster && e.Author == ClaimEventAuthor.Player => ClaimStatus.Discussed,
            // Скрытый от игрока комментарий статус не меняет.
            null when status is ClaimStatus.AddedByUser && masterVisibleToPlayer => ClaimStatus.Discussed,
            _ => status,
        };
    }

    /// <summary>Текст комментария об установке взноса — само число (<c>FinanceOperationsImpl.ChangeFee</c>).</summary>
    private static int? ParseFee(string text)
        => int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var fee) ? fee : null;

    /// <summary>«{старый} → {новый}» — текст комментария о смене ответственного (<c>ClaimServiceImpl.SetResponsible</c>).</summary>
    private static string? ParseNewResponsible(string text)
    {
        var arrowAt = text.LastIndexOf(ResponsibleArrow, StringComparison.Ordinal);
        if (arrowAt < 0)
        {
            return null;
        }
        var name = text[(arrowAt + ResponsibleArrow.Length)..].Trim();
        return name.Length == 0 ? null : name;
    }
}
