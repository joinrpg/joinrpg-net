using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Services.Notifications.Tests;

public class TemplaterTests
{
    private readonly Dictionary<string, string> FireFlyAndLeo = new() { { "game", "Firefly" }, { "name", "Leo" } };
    private readonly Dictionary<string, string> FireFlyAndRiver = new() { { "game", "Firefly" }, { "name", "River" } };

    [Fact]
    public void Helloworld()
    {
        var templater = new NotifcationFieldsTemplater(new NotificationEventTemplate("Hello, %recepient.name%!"));
        templater.GetFields().ShouldBe(["name"]);

        templater.Substitute(new Dictionary<string, string> { { "name", "Leo" } }).ShouldBe(new MarkdownString("Hello, Leo!"));
    }

    [Fact]
    public void Twice()
    {
        var templater = new NotifcationFieldsTemplater(new NotificationEventTemplate("Hello, %recepient.name%! Welcome to our %recepient.game%. Your name will be %recepient.name%."));
        templater.GetFields().ShouldBe(["game", "name"]);

        templater.Substitute(FireFlyAndLeo).ShouldBe(new MarkdownString("Hello, Leo! Welcome to our Firefly. Your name will be Leo."));
    }

    [Fact]
    public void CorrectlyReused()
    {
        var templater = new NotifcationFieldsTemplater(new NotificationEventTemplate("Hello, %recepient.name%! Welcome to our %recepient.game%."));
        templater.GetFields().ShouldBe(["game", "name"]);

        templater.Substitute(FireFlyAndLeo).ShouldBe(new MarkdownString("Hello, Leo! Welcome to our Firefly."));
        templater.Substitute(FireFlyAndRiver).ShouldBe(new MarkdownString("Hello, River! Welcome to our Firefly."));
    }
}
