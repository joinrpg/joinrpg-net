using static JoinRpg.Web.Comments.Test.CommentTestData;

namespace JoinRpg.Web.Comments.Test;

/// <summary>
/// По <see cref="DiscussionViewModel.HasUnread"/> страница решает, сдвигать ли отметку прочтения
/// (ADR026, шаг 6): непрочитанный ответ в глубине дерева тоже считается.
/// </summary>
public class DiscussionHasUnreadTest
{
    [Fact]
    public void EmptyDiscussion_HasNoUnread()
        => Discussion(hasMasterAccess: false).HasUnread().ShouldBeFalse();

    [Fact]
    public void AllRead_HasNoUnread()
        => Discussion(hasMasterAccess: false, Comment(1, replies: [Comment(2, depth: 1)]), Comment(3))
            .HasUnread().ShouldBeFalse();

    [Fact]
    public void UnreadRoot_IsUnread()
        => Discussion(hasMasterAccess: false, Comment(1), Comment(2, isRead: false))
            .HasUnread().ShouldBeTrue();

    [Fact]
    public void UnreadDeepReply_IsUnread()
        => Discussion(
                hasMasterAccess: false,
                Comment(1, replies: [Comment(2, depth: 1, replies: [Comment(3, isRead: false, depth: 2)])]))
            .HasUnread().ShouldBeTrue();
}
