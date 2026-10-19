using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Drp.ClaimNotifications;

namespace JoinRpg.Services.Drp.ClaimComments;

/// <summary>
/// Строит план восстановления комментариев к заявкам по уведомлениям (DRP, #5492): каких комментариев
/// после бэкапа нет в базе и с какой видимостью их вставить. Финансовые события не трогает — фаза 9.
/// </summary>
public static class CommentRestorePlanner
{
    /// <summary>
    /// Насколько может разойтись время комментария в базе и в уведомлении, чтобы считать их одним.
    /// Тексты комментариев базы старше бэкапа больше чем на это время планировщику не нужны.
    /// </summary>
    public static readonly TimeSpan SameTimeTolerance = TimeSpan.FromMinutes(1);

    private static readonly HashSet<CommentExtraAction> FinanceActions =
    [
        CommentExtraAction.ApproveFinance,
        CommentExtraAction.RejectFinance,
        CommentExtraAction.FeeChanged,
        CommentExtraAction.PaidFee,
        CommentExtraAction.RefundFee,
        CommentExtraAction.TransferTo,
        CommentExtraAction.TransferFrom,
        CommentExtraAction.RequestPreferential,
    ];

    /// <summary>
    /// Действия, которые меняют статус заявки, ответственного мастера или персонажа. Комментарий к ним
    /// всегда виден игроку — даже если уведомление игроку не пришло (#5641).
    /// </summary>
    private static readonly HashSet<CommentExtraAction> AlwaysVisibleActions =
    [
        CommentExtraAction.NewClaim,
        CommentExtraAction.ApproveByMaster,
        CommentExtraAction.DeclineByMaster,
        CommentExtraAction.DeclineByPlayer,
        CommentExtraAction.RestoreByMaster,
        CommentExtraAction.OnHoldByMaster,
        CommentExtraAction.CheckedIn,
        CommentExtraAction.SecondRole,
        CommentExtraAction.OutOfGame,
        CommentExtraAction.InvitationAcceptedByPlayer,
        CommentExtraAction.MoveByMaster,
        CommentExtraAction.ChangeResponsible,
    ];

    /// <returns>
    /// Строки по заявкам, у которых после бэкапа есть события, в порядке id заявки.
    /// </returns>
    public static IReadOnlyList<CommentRestorePlanRow> Plan(CommentRestoreSource source)
    {
        var commentsByClaim = source.ExistingComments.ToLookup(c => c.ClaimId);
        var existingIds = source.ExistingComments.ToDictionary(c => c.CommentId, c => c.ClaimId);
        var takenIds = source.TakenCommentIds.ToHashSet();
        var masters = source.Masters.ToHashSet();

        return [.. source.Events
            .GroupBy(e => e.ClaimId)
            .Select(g => PlanClaim(source, g.Key, Deduplicate(g), [.. commentsByClaim[g.Key]], existingIds, takenIds, masters))
            .OfType<CommentRestorePlanRow>()
            .OrderBy(r => r.ClaimId.ClaimId)];
    }

    private static CommentRestorePlanRow? PlanClaim(
        CommentRestoreSource source,
        ClaimIdentification claimId,
        IReadOnlyList<ClaimNotificationEvent> claimEvents,
        IReadOnlyList<ExistingClaimCommentInfo> existingComments,
        Dictionary<int, ClaimIdentification> existingIds,
        HashSet<int> takenIds,
        HashSet<UserIdentification> masters)
    {
        // Приглашения к проживанию заявку не меняют — ни комментариев, ни строки плана из них не будет.
        var afterBackup = claimEvents
            .Where(e => e.CreatedAt >= source.BackupTakenAt && e.Kind != ClaimNotificationKind.AccommodationInvite)
            .ToList();
        if (afterBackup.Count == 0)
        {
            return null;
        }

        var skippedFinance = afterBackup.Count(IsFinance);
        var unparsed = afterBackup.Count(e => e.Kind == ClaimNotificationKind.Other);
        var candidates = afterBackup
            .Where(e => e.Kind == ClaimNotificationKind.ClaimChange && !IsFinance(e))
            .OrderBy(e => e.CreatedAt)
            .ToList();

        var warnings = new List<CommentRestoreWarning>();
        if (!source.Claims.TryGetValue(claimId, out var player))
        {
            warnings.Add(CommentRestoreWarning.ClaimNotFound);
            AddUnparsedWarning(warnings, unparsed);
            return new CommentRestorePlanRow(claimId, ClaimExists: false, [], SkippedExisting: 0, Conflicts: [], skippedFinance, unparsed, warnings);
        }

        var history = new ClaimHistory(claimEvents, existingComments, player, playerIsMaster: masters.Contains(player));

        var toRestore = new List<CommentToRestore>();
        var conflicts = new List<int>();
        var skippedExisting = 0;
        foreach (var e in candidates)
        {
            if (e.CommentId is int commentId)
            {
                if (existingIds.TryGetValue(commentId, out var ownerClaim) && ownerClaim == claimId)
                {
                    skippedExisting++;
                    continue;
                }
                if (existingIds.ContainsKey(commentId) || takenIds.Contains(commentId))
                {
                    conflicts.Add(commentId);
                    continue;
                }
            }
            else if (existingComments.Any(c => IsSameComment(c, e)))
            {
                skippedExisting++;
                continue;
            }

            var byPlayer = e.Author == ClaimEventAuthor.Player;
            var visible = byPlayer || history.IsVisibleToPlayer(e);
            var questionable = !byPlayer && visible && !IsAlwaysVisible(e) && history.IsVisibilityQuestionable(e.CreatedAt);
            toRestore.Add(new CommentToRestore(e.CommentId, e.Initiator, byPlayer, e.Action, e.CreatedAt, e.Text, visible, questionable));
        }

        if (conflicts.Count > 0)
        {
            warnings.Add(CommentRestoreWarning.CommentIdConflict);
        }
        AddUnparsedWarning(warnings, unparsed);
        return new CommentRestorePlanRow(claimId, ClaimExists: true, toRestore, skippedExisting, conflicts, skippedFinance, unparsed, warnings);
    }

    private static void AddUnparsedWarning(List<CommentRestoreWarning> warnings, int unparsed)
    {
        if (unparsed > 0)
        {
            warnings.Add(CommentRestoreWarning.UnparsedEvents);
        }
    }

    /// <summary>Кто и когда писал в заявке — по всем событиям (и до бэкапа) и по базе.</summary>
    private sealed class ClaimHistory
    {
        private readonly UserIdentification player;
        private readonly bool playerIsMaster;
        private readonly List<DateTimeOffset> playerWrites;
        private readonly List<DateTimeOffset> visibleMasterWrites;

        public ClaimHistory(
            IReadOnlyList<ClaimNotificationEvent> events,
            IReadOnlyList<ExistingClaimCommentInfo> existingComments,
            UserIdentification player,
            bool playerIsMaster)
        {
            this.player = player;
            this.playerIsMaster = playerIsMaster;
            var changes = events.Where(e => e.Kind == ClaimNotificationKind.ClaimChange).ToList();
            playerWrites = [.. changes.Where(e => e.Author == ClaimEventAuthor.Player).Select(e => e.CreatedAt)
                .Concat(existingComments.Where(c => c.IsCommentByPlayer).Select(c => c.CreatedAt))];
            visibleMasterWrites = [.. changes.Where(e => e.Author == ClaimEventAuthor.Master && IsVisibleToPlayer(e)).Select(e => e.CreatedAt)
                .Concat(existingComments.Where(c => !c.IsCommentByPlayer && c.IsVisibleToPlayer).Select(c => c.CreatedAt))];
        }

        public bool IsVisibleToPlayer(ClaimNotificationEvent e) => IsAlwaysVisible(e) || e.Recipients.Any(r => r.UserId == player);

        /// <summary>
        /// Видимый мастерский комментарий мог быть и скрытым: игрок писал после предыдущего видимого
        /// комментария мастера (до фикса #5497 ему приходил и скрытый ответ) или игрок сам мастер проекта.
        /// </summary>
        public bool IsVisibilityQuestionable(DateTimeOffset at)
        {
            if (playerIsMaster)
            {
                return true;
            }
            var previousVisible = visibleMasterWrites.Where(t => t < at).DefaultIfEmpty(DateTimeOffset.MinValue).Max();
            return playerWrites.Any(t => t > previousVisible && t < at);
        }
    }

    /// <summary>
    /// Одно и то же событие не должно дать два кандидата: с id — по id, без id — по автору, времени,
    /// действию и тексту. Получатели копий объединяются — видимость по всем.
    /// </summary>
    private static List<ClaimNotificationEvent> Deduplicate(IEnumerable<ClaimNotificationEvent> events)
        => [.. events
            .GroupBy(e => e.CommentId is int id
                ? (object)(e.Kind, id)
                : (e.Kind, e.Initiator, e.CreatedAt, e.Action, Text: NormalizeText(e.Text)))
            .Select(g => g.First() with { Recipients = [.. g.SelectMany(e => e.Recipients).Distinct()] })];

    /// <summary>
    /// Комментарий без id в уведомлении совпадает с комментарием из базы. Комментарий без текста
    /// (не загружен — задолго до бэкапа) не совпадает ни с чем.
    /// </summary>
    private static bool IsSameComment(ExistingClaimCommentInfo existing, ClaimNotificationEvent e)
        => existing.AuthorUserId == e.Initiator
        && (existing.CreatedAt - e.CreatedAt).Duration() <= SameTimeTolerance
        && existing.Text is { } text
        && NormalizeText(text) == NormalizeText(e.Text);

    private static string NormalizeText(string text) => text.Replace("\r\n", "\n").Trim();

    private static bool IsAlwaysVisible(ClaimNotificationEvent e)
        => e.Action is { } action && AlwaysVisibleActions.Contains(action);

    private static bool IsFinance(ClaimNotificationEvent e)
        => e.Kind == ClaimNotificationKind.OnlinePayment
        || (e.Kind == ClaimNotificationKind.ClaimChange
            && (e.Money is not null
                || e.Reference is FinanceOperationIdentification
                || (e.Action is { } action && FinanceActions.Contains(action))));
}
