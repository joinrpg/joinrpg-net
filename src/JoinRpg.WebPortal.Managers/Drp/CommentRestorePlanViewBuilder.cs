using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Services.Drp.ClaimComments;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using UiCommentExtraAction = JoinRpg.CommonUI.Models.CommentExtraAction;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Перекладка между базой, планировщиком восстановления комментариев (#5492) и моделью страницы
/// (<see cref="CommentRestoreViewService"/>). Без обращений к базе — чтобы проверять тестами.
/// </summary>
internal static class CommentRestorePlanViewBuilder
{
    /// <summary>Id комментариев, которые встретились в уведомлениях, — их занятость надо проверить в базе.</summary>
    public static IReadOnlyCollection<int> NotifiedCommentIds(IEnumerable<ClaimNotificationEvent> events)
        => [.. events.Select(e => e.CommentId).OfType<int>().Distinct()];

    public static CommentRestoreSource BuildSource(
        ProjectInfo projectInfo,
        IReadOnlyCollection<ClaimNotificationEvent> events,
        ClaimCommentsRestoreLookup lookup,
        DateTimeOffset backupTakenAt)
        => new(
            events,
            lookup.Claims.ToDictionary(c => c.ClaimId, c => c.Player),
            [.. projectInfo.Masters.Select(m => m.UserId)],
            [.. lookup.Comments.Select(c => new ExistingClaimCommentInfo(
                c.CommentId, c.ClaimId, c.Author, c.CreatedAt, c.IsCommentByPlayer, c.IsVisibleToPlayer, c.Text))],
            lookup.TakenCommentIds,
            backupTakenAt);

    /// <summary>Пользователи, имена которых нужны для показа плана: авторы комментариев и игроки заявок.</summary>
    public static IReadOnlyCollection<UserIdentification> UsersToShow(IEnumerable<CommentRestorePlanRow> plan, ClaimCommentsRestoreLookup lookup)
    {
        var rows = plan.ToList();
        var claimIds = rows.Select(r => r.ClaimId).ToHashSet();
        return [.. rows.SelectMany(r => r.ToRestore.Select(c => c.Author))
            .Concat(lookup.Claims.Where(c => claimIds.Contains(c.ClaimId)).Select(c => c.Player))
            .Distinct()];
    }

    public static CommentRestorePlanViewModel Build(
        ProjectInfo projectInfo,
        IReadOnlyList<CommentRestorePlanRow> plan,
        ClaimCommentsRestoreLookup lookup,
        IReadOnlyCollection<UserInfoHeader> users)
    {
        var claims = lookup.Claims.ToDictionary(c => c.ClaimId);
        var names = users.ToDictionary(u => u.UserId, u => u.DisplayName.DisplayName);
        string NameOf(UserIdentification id) => names.TryGetValue(id, out var name) ? name : id.Value.ToString();

        var rows = plan.Select(row =>
        {
            var claim = claims.GetValueOrDefault(row.ClaimId);
            return new CommentRestoreRowViewModel(
                row.ClaimId,
                row.ClaimExists,
                claim?.CharacterName,
                claim is null ? null : NameOf(claim.Player),
                [.. row.ToRestore.Select(c => new CommentRestoreCommentView(
                    c.CommentId,
                    c.CreatedAt,
                    NameOf(c.Author),
                    c.IsCommentByPlayer,
                    c.IsVisibleToPlayer,
                    c.VisibilityQuestionable,
                    c.ExtraAction is { } action ? ((UiCommentExtraAction)action).GetDisplayName() : null,
                    new MarkdownString(c.Text).ToHtmlString().Value))],
                row.SkippedExisting,
                [.. row.Conflicts],
                row.SkippedFinance,
                row.Unparsed,
                [.. row.Warnings.Select(ToWarningView)]);
        }).ToList();

        return new CommentRestorePlanViewModel(projectInfo.ProjectId, UnavailableReason: null, rows);
    }

    public static CommentRestorePlanViewModel Unavailable(ProjectInfo projectInfo, ClaimRestoreUnavailableReason reason)
        => new(projectInfo.ProjectId, reason, []);

    /// <summary>Что отдать сервису восстановления — ровно комментарии плана.</summary>
    public static IReadOnlyList<RestoredClaimComment> ToRestore(CommentRestorePlanRow row)
        => [.. row.ToRestore.Select(c => new RestoredClaimComment(
            c.CommentId, c.Author, c.IsCommentByPlayer, c.IsVisibleToPlayer, c.ExtraAction, c.CreatedAt, c.Text))];

    internal static CommentRestoreWarningView ToWarningView(CommentRestoreWarning warning) => warning switch
    {
        CommentRestoreWarning.ClaimNotFound => CommentRestoreWarningView.ClaimNotFound,
        CommentRestoreWarning.CommentIdConflict => CommentRestoreWarningView.CommentIdConflict,
        CommentRestoreWarning.UnparsedEvents => CommentRestoreWarningView.UnparsedEvents,
        _ => throw new ArgumentOutOfRangeException(nameof(warning), warning, null),
    };
}
