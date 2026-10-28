using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces.Comments;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Comments;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Обсуждения в тестах — общий для всех тестовых проектов фейк поверх готовых снимков.
/// </summary>
/// <remarks>
/// Тест кладёт снимки и отметки прочтения сам (<see cref="Add"/>, <see cref="AddReadWatermark"/>);
/// чего не положил, того нет — как и в БД, ответ «не найдено», а не исключение. Отметки на пару
/// «обсуждение, пользователь» может быть несколько, отдаётся максимальная — как в БД.
/// В отличие от соседних фейков, этот не строится от <see cref="MockedProject"/>: снимки собирает
/// тест, и согласованность с моком (заявки, авторы) — на нём.
/// </remarks>
public sealed class FakeCommentDiscussionRepository : ICommentDiscussionRepository
{
    private readonly Dictionary<CommentDiscussionId, CommentDiscussionInfo> discussions = [];
    private readonly List<(CommentDiscussionId DiscussionId, UserIdentification UserId, CommentId CommentId)> watermarks = [];

    public FakeCommentDiscussionRepository(params CommentDiscussionInfo[] discussions)
    {
        foreach (var discussion in discussions)
        {
            _ = Add(discussion);
        }
    }

    public FakeCommentDiscussionRepository Add(CommentDiscussionInfo discussion)
    {
        discussions.Add(discussion.Id, discussion);
        return this;
    }

    public FakeCommentDiscussionRepository AddReadWatermark(
        CommentDiscussionId discussionId,
        UserIdentification userId,
        CommentId commentId)
    {
        watermarks.Add((discussionId, userId, commentId));
        return this;
    }

    public Task<CommentDiscussionInfo?> GetDiscussionOrDefault(CommentDiscussionId discussionId)
        => Task.FromResult(discussions.GetValueOrDefault(discussionId));

    public Task<CommentDiscussionOwner?> GetOwnerOrDefault(CommentDiscussionId discussionId)
        => Task.FromResult(discussions.GetValueOrDefault(discussionId)?.Owner);

    public Task<CommentDiscussionOwner?> GetOwnerByCommentOrDefault(CommentId commentId)
        => Task.FromResult(
            discussions.Values
                .SingleOrDefault(discussion => discussion.Comments.Any(comment => comment.Id == commentId))
                ?.Owner);

    public Task<CommentId?> GetReadWatermark(CommentDiscussionId discussionId, UserIdentification userId)
        => Task.FromResult(
            watermarks
                .Where(mark => mark.DiscussionId == discussionId && mark.UserId == userId)
                .Select(mark => mark.CommentId)
                .MaxBy(commentId => commentId.Value));
}
