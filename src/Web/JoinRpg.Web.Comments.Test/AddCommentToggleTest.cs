using static JoinRpg.Web.Comments.Test.CommentTestData;

namespace JoinRpg.Web.Comments.Test;

/// <summary>
/// Тоггл формы комментария: нативный <c>&lt;details&gt;</c> (без панели — по тогглу у каждого комментария),
/// заголовок зависит от режима, <c>ReplyTo</c> доходит до формы (ADR026 §3).
/// </summary>
public class AddCommentToggleTest : BunitContext
{
    public AddCommentToggleTest() => this.AddCommentServices();

    [Fact]
    public void ReplyMode_ShowsReplyHeader_AndPostsParent()
    {
        var parent = Comment(10);

        var toggle = Render<AddCommentToggle>(parameters => parameters
            .Add(c => c.Discussion, Discussion(hasMasterAccess: true, parent))
            .Add(c => c.ReplyTo, parent));

        toggle.Find("summary").TextContent.ShouldBe("Ответить");
        // ReplyTo дошла до формы: ответ постится с ParentCommentId.
        toggle.Find("form").QuerySelector("input[name=ParentCommentId]").ShouldNotBeNull().GetAttribute("value").ShouldBe("10");
    }

    [Fact]
    public void NewCommentMode_ShowsAddHeader_AndHasNoParent()
    {
        var toggle = Render<AddCommentToggle>(parameters => parameters
            .Add(c => c.Discussion, Discussion(hasMasterAccess: true)));

        toggle.Find("summary").TextContent.ShouldBe("Добавить комментарий");
        toggle.Find("form").QuerySelectorAll("input[name=ParentCommentId]").ShouldBeEmpty();
    }
}
