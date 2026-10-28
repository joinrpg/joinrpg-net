using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Дерево комментариев — снимок на момент построения: контроллеры сдвигают отметку прочтения
/// уже после него (ADR026, шаг 3), и пометка «новое» не должна от этого меняться.
/// </summary>
public class CommentViewModelTest
{
    private readonly MockedProject mock = new();

    [Fact]
    public void CommentTree_WatermarkMovedAfterBuild_ReplyStillUnread()
    {
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Персонаж"), mock.Player);
        claim.CommentDiscussion.Watermarks = [];
        var root = mock.CreateComment(claim, "Корень");
        var reply = mock.CreateComment(claim, "Ответ");
        reply.ParentCommentId = root.CommentId;

        var tree = claim.CommentDiscussion.ToCommentTreeViewModel(new UserIdentification(mock.Player.UserId), mock.ProjectInfo);

        // Так отметку сдвигает UpdateReadCommentWatermark, когда граф виден тому же контексту.
        claim.CommentDiscussion.Watermarks.Add(new ReadCommentWatermark
        {
            CommentDiscussionId = claim.CommentDiscussionId,
            ProjectId = claim.ProjectId,
            UserId = mock.Player.UserId,
            CommentId = reply.CommentId,
        });

        var rootModel = tree.ShouldHaveSingleItem();
        rootModel.IsRead.ShouldBeFalse();
        rootModel.ChildComments.ShouldHaveSingleItem().IsRead.ShouldBeFalse();
    }
}
