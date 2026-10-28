using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.Forums;

namespace JoinRpg.DomainTypes.Test.Comments;

public class CommentDiscussionInfoTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ClaimDiscussionOwner ClaimOwner = new(new ClaimIdentification(ProjectId, 10));
    private static readonly DateTime Day = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CommentInfo MakeComment(int id, DateTime createdAt)
        => new(
            new CommentId(id),
            ParentCommentId: null,
            new UserIdentification(5),
            createdAt,
            IsCommentByPlayer: false,
            IsVisibleToPlayer: true,
            ExtraAction: null,
            new MarkdownString("текст"),
            Finance: null);

    [Fact]
    public void CommentsAreOrderedByCreatedAtThenById()
    {
        // Восстановленный DRP комментарий (id 100) — со старым временем, поэтому раньше id 2.
        // Комментарии форума старше PR 2c хранят только дату: внутри дня порядок держит id.
        var discussion = new CommentDiscussionInfo(
            new CommentDiscussionId(7),
            ClaimOwner,
            [
                MakeComment(3, Day.AddHours(1)),
                MakeComment(2, Day.AddHours(2)),
                MakeComment(100, Day),
                MakeComment(1, Day),
            ]);

        discussion.Comments.Select(comment => comment.Id.Value).ShouldBe([1, 100, 3, 2]);
    }

    [Fact]
    public void ClaimOwnerBuildsClaimCommentIdentification()
    {
        CommentDiscussionOwner owner = ClaimOwner;

        owner.ProjectId.ShouldBe(ProjectId);
        owner.GetCommentIdentification(new CommentId(42))
            .ShouldBe(new ClaimCommentIdentification(ClaimOwner.ClaimId, 42));
    }

    [Fact]
    public void ForumOwnerBuildsForumCommentIdentification()
    {
        var threadId = new ForumThreadIdentification(ProjectId, 3);
        CommentDiscussionOwner owner = new ForumDiscussionOwner(
            threadId,
            new CharacterGroupIdentification(ProjectId, 4),
            IsVisibleToPlayer: true);

        owner.ProjectId.ShouldBe(ProjectId);
        owner.GetCommentIdentification(new CommentId(42))
            .ShouldBe(new ForumCommentIdentification(threadId, 42));
    }

    [Fact]
    public void ForumOwnerRejectsGroupFromAnotherProject()
        => Should.Throw<ArgumentException>(() => new ForumDiscussionOwner(
            new ForumThreadIdentification(ProjectId, 3),
            new CharacterGroupIdentification(new ProjectIdentification(2), 4),
            IsVisibleToPlayer: true));
}
