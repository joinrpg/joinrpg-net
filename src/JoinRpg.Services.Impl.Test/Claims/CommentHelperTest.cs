using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Test.Claims;

public class CommentHelperTest
{
    [Fact]
    public void ForumCommentKeepsTimeOfDay()
    {
        var mock = new MockedProject();
        var helper = new CommentHelper(new FakeCurrentUserAccessor(mock.Master.UserId));
        var forumThread = new ForumThread
        {
            ProjectId = mock.Project.ProjectId,
            CommentDiscussion = new CommentDiscussion { ProjectId = mock.Project.ProjectId },
        };
        var createdAt = new DateTime(2026, 10, 28, 14, 35, 17, DateTimeKind.Utc);

        var comment = helper.CreateCommentForForumThread(
            forumThread,
            createdAt,
            "Текст",
            mock.ProjectInfo,
            ClaimOperationType.MasterVisibleChange);

        comment.CreatedAt.ShouldBe(createdAt);
        comment.LastEditTime.ShouldBe(createdAt);
    }
}
