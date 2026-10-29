using AngleSharp.Dom;
using static JoinRpg.Web.Comments.Test.CommentTestData;

namespace JoinRpg.Web.Comments.Test;

/// <summary>
/// Форма комментария: контракт с <c>ForumController.CreateComment</c> (поля как у
/// <c>_AddCommentPartial.cshtml</c>) и правило «ответ на скрытый — тоже скрыт» (ADR026 §3).
/// </summary>
public class AddCommentFormTest : BunitContext
{
    public AddCommentFormTest() => this.AddCommentServices();

    private IElement RenderForm(DiscussionViewModel discussion, CommentViewModel? replyTo = null)
        => Render<AddCommentForm>(parameters => parameters
            .Add(c => c.Discussion, discussion)
            .Add(c => c.ReplyTo, replyTo))
        .Find("form");

    private static IHtmlCollection<IElement> HideFromUserFields(IElement form)
        => form.QuerySelectorAll("[name=HideFromUser]");

    /// <summary>
    /// Что браузер отправит в HideFromUser: отмеченный и не отключённый чекбокс плюс скрытые поля, по порядку.
    /// MVC биндит bool по первому значению.
    /// </summary>
    private static string?[] PostedHideFromUser(IElement form)
        => [.. HideFromUserFields(form)
            .Where(field => field.GetAttribute("type") == "hidden"
                || (field.HasAttribute("checked") && !field.HasAttribute("disabled")))
            .Select(field => field.GetAttribute("value"))];

    [Fact]
    public void ReplyToHiddenComment_ForcesHideFromUser()
    {
        // Видимый ответ на скрытый сервис отклонит, а форма молча вернёт в обсуждение — текст потеряется.
        var hidden = Comment(10, isVisibleToPlayer: false);

        var form = RenderForm(Discussion(hasMasterAccess: true, hidden), hidden);

        PostedHideFromUser(form).First().ShouldBe("true");
        var checkbox = form.QuerySelector("input[type=checkbox][name=HideFromUser]").ShouldNotBeNull();
        checkbox.HasAttribute("checked").ShouldBeTrue();
        checkbox.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void ReplyToVisibleComment_MasterChoosesFreely()
    {
        var visible = Comment(10);

        var form = RenderForm(Discussion(hasMasterAccess: true, visible), visible);

        var checkbox = form.QuerySelector("input[type=checkbox][name=HideFromUser]").ShouldNotBeNull();
        checkbox.GetAttribute("value").ShouldBe("true");
        checkbox.HasAttribute("checked").ShouldBeFalse();
        checkbox.HasAttribute("disabled").ShouldBeFalse();
        PostedHideFromUser(form).First().ShouldBe("false");
    }

    [Fact]
    public void Player_CannotHideFromUser()
    {
        var form = RenderForm(Discussion(hasMasterAccess: false, Comment(10)), Comment(10));

        HideFromUserFields(form).ShouldBeEmpty();
    }

    [Fact]
    public void Reply_PostsToCreateCommentWithParent()
    {
        var parent = Comment(10);

        var form = RenderForm(Discussion(hasMasterAccess: true, parent), parent);

        form.GetAttribute("method").ShouldBe("post");
        form.GetAttribute("action").ShouldBe("https://test/123/forums/createcomment");
        FieldValue(form, "ProjectId").ShouldBe("123");
        FieldValue(form, "CommentDiscussionId").ShouldBe("45");
        FieldValue(form, "ParentCommentId").ShouldBe("10");
        var text = form.QuerySelector("textarea[name=CommentText]").ShouldNotBeNull();
        text.HasAttribute("required").ShouldBeTrue();
        text.Id.ShouldNotBeNullOrEmpty();
        form.QuerySelector($"label[for='{text.Id}']").ShouldNotBeNull();
        form.QuerySelectorAll("button[type=submit]").ShouldHaveSingleItem();
        // «Отмена» сворачивает панель через общий скрипт Common.WebComponents — по этому атрибуту.
        form.QuerySelectorAll("button[type=button][data-join-collapse-close]").ShouldHaveSingleItem().TextContent.ShouldContain("Отмена");
        form.QuerySelector("input[name=__RequestVerificationToken]").ShouldNotBeNull();
    }

    [Fact]
    public void NewComment_HasNoParent()
    {
        var form = RenderForm(Discussion(hasMasterAccess: true));

        form.QuerySelectorAll("[name=ParentCommentId]").ShouldBeEmpty();
        FieldValue(form, "CommentDiscussionId").ShouldBe("45");
    }

    private static string? FieldValue(IElement form, string name)
        => form.QuerySelectorAll($"input[name={name}]").ShouldHaveSingleItem().GetAttribute("value");
}
