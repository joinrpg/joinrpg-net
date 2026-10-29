using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using static JoinRpg.Web.Comments.Test.CommentTestData;

namespace JoinRpg.Web.Comments.Test;

/// <summary>
/// Дерево обсуждения: пометка «новое», вложенность ответов, слот доп. содержимого, скрытие от игрока.
/// </summary>
public class DiscussionTest : BunitContext
{
    public DiscussionTest() => this.AddCommentServices();

    private IRenderedComponent<Discussion> RenderDiscussion(
        DiscussionViewModel model,
        RenderFragment<CommentViewModel>? extraContent = null)
        => Render<Discussion>(parameters => parameters
            .Add(c => c.Model, model)
            .Add(c => c.ExtraContent, extraContent));

    private static IElement CommentBlock(IRenderedComponent<Discussion> cut, int commentId)
        => cut.Find($"#comment{commentId}");

    /// <summary>Шапка самого комментария — без его ответов.</summary>
    private static IElement OwnHeader(IRenderedComponent<Discussion> cut, int commentId)
        => CommentBlock(cut, commentId).QuerySelector(".join-comment__row").ShouldNotBeNull();

    [Fact]
    public void UnreadComment_IsMarkedNew_ReadIsNot()
    {
        var cut = RenderDiscussion(Discussion(
            hasMasterAccess: false,
            Comment(1, isRead: true),
            Comment(2, isRead: false)));

        OwnHeader(cut, 1).TextContent.ShouldNotContain("New");
        OwnHeader(cut, 2).TextContent.ShouldContain("New");
    }

    [Fact]
    public void UnreadReply_IsMarkedNew_UnderReadParent()
    {
        // Ответ — отдельный комментарий со своей пометкой: прочитанный родитель её не гасит.
        var cut = RenderDiscussion(Discussion(
            hasMasterAccess: false,
            Comment(1, isRead: true, replies: [Comment(2, isRead: false, depth: 1)])));

        OwnHeader(cut, 1).TextContent.ShouldNotContain("New");
        OwnHeader(cut, 2).TextContent.ShouldContain("New");
    }

    [Fact]
    public void Replies_AreNestedInsideParent_InModelOrder()
    {
        var cut = RenderDiscussion(Discussion(
            hasMasterAccess: false,
            Comment(1, replies: [Comment(3, depth: 1), Comment(2, depth: 1)]),
            Comment(4)));

        var parent = CommentBlock(cut, 1);
        parent.QuerySelectorAll("[id^=comment]").Select(e => e.Id).ShouldBe(["comment3", "comment2"]);
        CommentBlock(cut, 4).Contains(CommentBlock(cut, 3)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(CommentViewModel.MaxIndentedDepth, true)]
    [InlineData(CommentViewModel.MaxIndentedDepth + 1, false)]
    public void DeepReplies_StopIndenting(int depth, bool isIndented)
        => Comment(1, depth: depth).IsIndented.ShouldBe(isIndented);

    [Fact]
    public void ReplyFormInTree_ForHiddenComment_ForcesHideFromUser()
    {
        var cut = RenderDiscussion(Discussion(
            hasMasterAccess: true,
            Comment(1, isVisibleToPlayer: false),
            Comment(2)));

        OwnHeader(cut, 1).QuerySelector(".join-collapse-panel form input[type=checkbox][name=HideFromUser]")
            .ShouldNotBeNull().HasAttribute("disabled").ShouldBeTrue();
        OwnHeader(cut, 2).QuerySelector(".join-collapse-panel form input[type=checkbox][name=HideFromUser]")
            .ShouldNotBeNull().HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Avatar_ShownOnlyWhenPresent()
    {
        var cut = RenderDiscussion(Discussion(
            hasMasterAccess: false,
            Comment(1),
            Comment(2) with { AuthorAvatarUri = null }));

        OwnHeader(cut, 1).QuerySelector("img").ShouldNotBeNull().GetAttribute("src").ShouldBe("https://avatars.example.com/7.png");
        OwnHeader(cut, 2).QuerySelector("img").ShouldBeNull();
    }

    [Fact]
    public void ExtraActionText_IsShown()
    {
        var cut = RenderDiscussion(Discussion(
            hasMasterAccess: false,
            Comment(1) with { ExtraActionText = "Заявка одобрена мастером" }));

        OwnHeader(cut, 1).TextContent.ShouldContain("Заявка одобрена мастером");
    }

    [Fact]
    public void ExtraContent_IsRenderedForEachComment()
    {
        var cut = RenderDiscussion(
            Discussion(hasMasterAccess: false, Comment(1, replies: [Comment(2, depth: 1)])),
            comment => builder => builder.AddMarkupContent(0, $"<span class=\"extra\">доп {comment.CommentId.Value}</span>"));

        OwnHeader(cut, 1).QuerySelector(".extra").ShouldNotBeNull().TextContent.ShouldBe("доп 1");
        OwnHeader(cut, 2).QuerySelector(".extra").ShouldNotBeNull().TextContent.ShouldBe("доп 2");
    }

    [Fact]
    public void HiddenComment_IsMarkedForMaster()
    {
        var cut = RenderDiscussion(Discussion(hasMasterAccess: true, Comment(1, isVisibleToPlayer: false)));

        OwnHeader(cut, 1).QuerySelector(".world-object-hidden").ShouldNotBeNull();
        OwnHeader(cut, 1).TextContent.ShouldContain("другим мастерам");
    }

    [Fact]
    public void ConcealableComment_PostsConcealWithCommentId()
    {
        var cut = RenderDiscussion(Discussion(hasMasterAccess: true, Comment(1, canConceal: true), Comment(2)));

        var form = OwnHeader(cut, 1).QuerySelector("form[action$=concealcomment]").ShouldNotBeNull();
        // ForumController.ConcealComment(projectid из маршрута, CommentId? commentid) — других полей нет.
        form.QuerySelectorAll("input[type=hidden]:not([name=__RequestVerificationToken])")
            .ShouldHaveSingleItem().GetAttribute("name").ShouldBe("commentId");
        form.QuerySelector("input[name=commentId]")!.GetAttribute("value").ShouldBe("1");
        OwnHeader(cut, 2).QuerySelector("form[action$=concealcomment]").ShouldBeNull();
    }

    [Fact]
    public void AuthorLine_IsNotGluedTogether()
    {
        // Blazor вырезает пробелы между тегами — без @preservewhitespace «Мастер», имя и «написал(а)» слиплись бы.
        var cut = RenderDiscussion(Discussion(hasMasterAccess: false, Comment(1)));

        var header = OwnHeader(cut, 1).QuerySelector(".join-comment__header")!.TextContent;
        string.Join(' ', header.Split((char[])[' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries))
            .ShouldStartWith("Мастер Автор написал(а)");
    }
}
