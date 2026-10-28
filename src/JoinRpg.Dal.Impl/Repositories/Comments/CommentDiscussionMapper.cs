using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Dal.Impl.Repositories.Comments;

/// <summary>
/// Чистый маппинг строк <see cref="CommentDiscussionRepository"/> в доменный снимок обсуждения
/// (ADR026 §2). БД не трогает — юнит-тестируется на готовых строках.
/// </summary>
internal static class CommentDiscussionMapper
{
    public static CommentDiscussionInfo Map(CommentDiscussionRow row)
        => new(
            new CommentDiscussionId(row.CommentDiscussionId),
            MapOwner(row),
            [.. row.Comments.Select(comment => MapComment(comment, new ProjectIdentification(row.ProjectId)))]);

    /// <exception cref="InvalidOperationException">
    /// У обсуждения нет владельца, их два или проект владельца не совпал с проектом обсуждения —
    /// данные в БД рассогласованы.
    /// </exception>
    /// <remarks>
    /// Адрес владельца строится от проекта самой заявки или темы, а не обсуждения: по нему
    /// вызывающие защищаются от межпроектного доступа (ADR026 §1), и рассогласование не должно
    /// пройти молча.
    /// </remarks>
    public static CommentDiscussionOwner MapOwner(ICommentDiscussionOwnerRow row)
    {
        return (row.ClaimId, row.ForumThreadId) switch
        {
            ({ } claimId, null) => new ClaimDiscussionOwner(
                new ClaimIdentification(OwnerProject(row, row.ClaimProjectId), claimId)),
            (null, { } threadId) => new ForumDiscussionOwner(
                new ForumThreadIdentification(OwnerProject(row, row.ForumThreadProjectId), threadId),
                new CharacterGroupIdentification(
                    OwnerProject(row, row.ForumThreadProjectId),
                    row.ForumThreadCharacterGroupId
                        ?? throw new InvalidOperationException($"Forum thread {threadId} has no character group")),
                row.ForumThreadIsVisibleToPlayer
                    ?? throw new InvalidOperationException($"Forum thread {threadId} has no visibility")),
            (null, null) => throw new InvalidOperationException(
                $"Comment discussion {row.CommentDiscussionId} has neither claim nor forum thread"),
            _ => throw new InvalidOperationException(
                $"Comment discussion {row.CommentDiscussionId} belongs both to claim {row.ClaimId} and to forum thread {row.ForumThreadId}"),
        };
    }

    private static ProjectIdentification OwnerProject(ICommentDiscussionOwnerRow row, int? ownerProjectId)
        => ownerProjectId == row.ProjectId
            ? new ProjectIdentification(row.ProjectId)
            : throw new InvalidOperationException(
                $"Comment discussion {row.CommentDiscussionId} is in project {row.ProjectId}, but its owner is in project {ownerProjectId}");

    private static CommentInfo MapComment(CommentRow row, ProjectIdentification projectId)
        => new(
            new CommentId(row.CommentId),
            CommentId.FromOptional(row.ParentCommentId),
            new UserIdentification(row.AuthorUserId),
            row.CreatedAt,
            row.IsCommentByPlayer,
            row.IsVisibleToPlayer,
            row.ExtraAction,
            row.Text is null ? null : new MarkdownString(row.Text),
            MapFinance(row, projectId));

    private static CommentFinanceInfo? MapFinance(CommentRow row, ProjectIdentification projectId)
    {
        if (row.FinanceOperationType is not { } operationType)
        {
            return null;
        }

        return new CommentFinanceInfo(
            row.FinanceMoneyAmount ?? throw new InvalidOperationException($"Finance operation {row.CommentId} has no amount"),
            operationType,
            row.FinanceState ?? throw new InvalidOperationException($"Finance operation {row.CommentId} has no state"),
            row.FinanceOperationDate ?? throw new InvalidOperationException($"Finance operation {row.CommentId} has no date"),
            row.FinancePaymentTypeId is { } paymentTypeId ? new PaymentTypeIdentification(projectId, paymentTypeId) : null);
    }
}
