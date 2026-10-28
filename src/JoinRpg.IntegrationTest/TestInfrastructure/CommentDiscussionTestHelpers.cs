using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Комментарии к заявке и теме через единый вход обсуждения (<see cref="ICommentDiscussionService"/>,
/// ADR026 §5). Сценариям удобнее заявка или тема, а вход принимает id обсуждения — его хелпер берёт из БД.
/// </summary>
internal static class CommentDiscussionTestHelpers
{
    /// <summary>Комментарий к заявке от текущего пользователя провайдера.</summary>
    public static Task AddClaimComment(
        this IServiceProvider services,
        ClaimIdentification claimId,
        CommentId? parentCommentId,
        bool isVisibleToPlayer,
        string commentText)
    {
        var discussionId = services.GetRequiredService<MyDbContext>().Set<Claim>()
            .Where(c => c.ClaimId == claimId.ClaimId)
            .Select(c => c.CommentDiscussionId)
            .Single();
        return services.GetRequiredService<ICommentDiscussionService>()
            .AddComment(new CommentDiscussionId(discussionId), parentCommentId, isVisibleToPlayer, commentText);
    }

    /// <summary>Комментарий к теме форума от текущего пользователя провайдера.</summary>
    public static Task AddThreadComment(
        this IServiceProvider services,
        ForumThreadIdentification threadId,
        CommentId? parentCommentId,
        bool isVisibleToPlayer,
        string commentText)
    {
        var discussionId = services.GetRequiredService<MyDbContext>().Set<ForumThread>()
            .Where(t => t.ForumThreadId == threadId.ThreadId)
            .Select(t => t.CommentDiscussionId)
            .Single();
        return services.GetRequiredService<ICommentDiscussionService>()
            .AddComment(new CommentDiscussionId(discussionId), parentCommentId, isVisibleToPlayer, commentText);
    }
}
