using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Выражение «непрочитано» для SQL и правило в памяти (<see cref="CommentExtensions.GetUnreadCount"/>)
/// должны давать одни числа (ADR026, §2). На живой базе то же проверяет <c>UnreadCommentsCountScenario</c>.
/// </summary>
public class CommentDiscussionPredicatesTest
{
    private static readonly MockedProject Mock = new();
    private const int PlayerId = 1;
    private const int MasterId = 2;
    private const int OtherId = 3;
    private const int ReaderId = 4;

    private static readonly Func<CommentDiscussion, int, bool, int> UnreadCount =
        CommentDiscussionPredicates.UnreadCount.Compile();

    private static int Unread(CommentDiscussion discussion, int userId)
        => UnreadCount(discussion, userId, Mock.ProjectInfo.HasMasterAccess(new UserIdentification(userId)));

    private static CommentDiscussion CreateDiscussion(
        IEnumerable<(int Author, bool Visible)> comments,
        IEnumerable<(int User, int CommentIndex)> watermarks)
    {
        var commentList = comments
            .Select((c, index) => new Comment { CommentId = (index + 1) * 10, AuthorUserId = c.Author, IsVisibleToPlayer = c.Visible })
            .ToList();
        return new CommentDiscussion
        {
            Comments = commentList,
            Watermarks = [.. watermarks.Select(w => new ReadCommentWatermark { UserId = w.User, CommentId = commentList[w.CommentIndex].CommentId })],
        };
    }

    [Fact]
    public void MockMasterAccess_IsAsExpected()
    {
        // Мок согласован с тестом: игрок — не мастер, мастер — мастер.
        Mock.ProjectInfo.HasMasterAccess(new UserIdentification(PlayerId)).ShouldBeFalse();
        Mock.ProjectInfo.HasMasterAccess(new UserIdentification(MasterId)).ShouldBeTrue();
    }

    [Fact]
    public void EmptyDiscussion_HasNothingUnread()
    {
        var discussion = CreateDiscussion([], []);

        Unread(discussion, PlayerId).ShouldBe(0);
        Unread(discussion, MasterId).ShouldBe(0);
    }

    [Fact]
    public void HiddenComments_AreUnreadOnlyForMaster()
    {
        var discussion = CreateDiscussion([(OtherId, true), (OtherId, false), (OtherId, false)], []);

        Unread(discussion, PlayerId).ShouldBe(1);
        Unread(discussion, MasterId).ShouldBe(3);
    }

    [Fact]
    public void OwnComment_MarksEverythingBeforeAsRead()
    {
        var discussion = CreateDiscussion([(OtherId, true), (OtherId, true), (PlayerId, true), (OtherId, true)], []);

        Unread(discussion, PlayerId).ShouldBe(1);
    }

    [Fact]
    public void Watermark_CountsMaximumAmongDuplicatesAndOwnComments()
    {
        var discussion = CreateDiscussion(
            [(OtherId, true), (PlayerId, true), (OtherId, true), (OtherId, true), (OtherId, true)],
            [(PlayerId, 3), (PlayerId, 3), (PlayerId, 0), (MasterId, 4)]);

        // Отметка игрока — c4 (максимум из дублей и отметки ниже), его комментарий c2 ниже неё.
        Unread(discussion, PlayerId).ShouldBe(1);
        // Чужие отметки и чужие комментарии не считаются.
        Unread(discussion, ReaderId).ShouldBe(5);
    }

    [Fact]
    public void Expression_MatchesInMemoryRule_OnRandomDiscussions()
    {
        var random = new Random(4986);
        int[] users = [PlayerId, MasterId, OtherId];
        for (var i = 0; i < 500; i++)
        {
            var comments = Enumerable.Range(0, random.Next(0, 10))
                .Select(_ => (Author: users[random.Next(users.Length)], Visible: random.Next(3) > 0))
                .ToList();
            var watermarks = comments.Count == 0
                ? []
                : Enumerable.Range(0, random.Next(0, 5))
                    .Select(_ => (User: users[random.Next(users.Length)], CommentIndex: random.Next(comments.Count)))
                    .ToList();
            var discussion = CreateDiscussion(comments, watermarks);

            foreach (var userId in (int[])[.. users, ReaderId])
            {
                Unread(discussion, userId).ShouldBe(discussion.GetUnreadCount(userId, Mock.ProjectInfo), $"обсуждение {i}, пользователь {userId}");
            }
        }
    }
}
