using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Helpers;

namespace JoinRpg.WebPortal.Models.Test;

public class RendererIntegratedTest
{
    [Fact]
    public void RenderCharacterHtml()
    {
        var projectMock = new MockedProject();
        var renderer =
            new JoinrpgMarkdownLinkRenderer(projectMock.Project, projectMock.ProjectInfo);

        var ch = projectMock.CreateCharacter("Элендил");

        var text = new MarkdownString($"%персонаж{ch.CharacterId}");

        text.ToHtmlString(renderer).Value
            .ShouldBe("<p><a href=\"/0/character/2\" target=\"_blank\" rel=\"nofollow\">Элендил</a></p>");
    }

    [Fact]
    public void RenderCharacterText()
    {
        var projectMock = new MockedProject();
        var renderer =
            new JoinrpgMarkdownLinkRenderer(projectMock.Project, projectMock.ProjectInfo);

        var ch = projectMock.CreateCharacter("Элендил");

        var text = new MarkdownString($"%персонаж{ch.CharacterId}");

        text.ToPlainTextWithoutHtmlEscape(renderer)
            .ShouldBe("Элендил");
    }

    [Fact]
    public void RenderCharacterHtmlWithExtra()
    {
        var projectMock = new MockedProject();
        var renderer =
            new JoinrpgMarkdownLinkRenderer(projectMock.Project, projectMock.ProjectInfo);

        var ch = projectMock.CreateCharacter("Элендил");

        var text = new MarkdownString($"%персонаж{ch.CharacterId}(принца Элендила)");

        text.ToHtmlString(renderer).Value
            .ShouldBe("<p><a href=\"/0/character/2\" target=\"_blank\" rel=\"nofollow\">принца Элендила</a></p>");
    }

    [Fact]
    public void RenderCharacterTextWithExtra()
    {
        var projectMock = new MockedProject();
        var renderer =
            new JoinrpgMarkdownLinkRenderer(projectMock.Project, projectMock.ProjectInfo);

        var ch = projectMock.CreateCharacter("Элендил");

        var text = new MarkdownString($"%персонаж{ch.CharacterId}(принца Элендила)");

        text.ToPlainTextWithoutHtmlEscape(renderer)
            .ShouldBe("принца Элендила");
    }

    [Fact]
    public void RenderGroupHtml()
    {
        var projectMock = new MockedProject();
        var renderer =
            new JoinrpgMarkdownLinkRenderer(projectMock.Project, projectMock.ProjectInfo);


        var text = new MarkdownString($"%группа{projectMock.Group.CharacterGroupId}");

        text.ToHtmlString(renderer).Value
            .ShouldBe("<p><a href=\"/0/roles/2/details\" target=\"_blank\" rel=\"nofollow\">test_2</a></p>");
    }
}
