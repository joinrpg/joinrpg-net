namespace JoinRpg.Services.Notifications.Tests;

public class TemplaterTests
{
    private readonly Dictionary<string, string> EmptyDict = [];

    [Fact]
    public void Helloworld()
    {
        var templater = new NotifcationFieldsTemplater(new MarkdownString("Hello, %recepient.name%!"));
        templater.GetFields().ShouldBe(["name"]);

        templater.Substitute(EmptyDict, new UserDisplayName("Leo", null)).ShouldBe(new MarkdownString("Hello, Leo!"));
    }

    [Fact]
    public void Twice()
    {
        var templater = new NotifcationFieldsTemplater(new MarkdownString("Hello, %recepient.name%! Welcome to our %recepient.game%. Your name will be %recepient.name%."));
        templater.GetFields().ShouldBe(["game", "name"]);

        templater.Substitute(new Dictionary<string, string> { { "game", "Firefly" } }, new UserDisplayName("Leo", null)).ShouldBe(new MarkdownString("Hello, Leo! Welcome to our Firefly. Your name will be Leo."));
    }

    [Fact]
    public void CorrectlyReused()
    {
        var templater = new NotifcationFieldsTemplater(new MarkdownString("Hello, %recepient.name%! Welcome to our %recepient.game%."));
        templater.GetFields().ShouldBe(["game", "name"]);

        var fields = new Dictionary<string, string> { { "game", "Firefly" } };
        templater.Substitute(fields, new UserDisplayName("Leo", null)).ShouldBe(new MarkdownString("Hello, Leo! Welcome to our Firefly."));
        templater.Substitute(fields, new UserDisplayName("River", null)).ShouldBe(new MarkdownString("Hello, River! Welcome to our Firefly."));
    }
}
