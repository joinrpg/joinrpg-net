using JoinRpg.Common.PrimitiveTypes;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Common.WebComponents.Test;

public class JoinUserLinkEditorTest
{
    private const int DemoProjectId = 17;

    /// <summary>
    /// Редактор всегда инжектит клиент резолва, поэтому контекст готовим одинаково для всех тестов.
    /// Резолв при этом включается только параметром <c>ProjectId</c>.
    /// </summary>
    private static BunitContext CreateContext(FakeUserLinkResolveClient? client = null)
    {
        var ctx = new BunitContext();
        _ = ctx.Services.AddSingleton<IUserLinkResolveClient>(client ?? new FakeUserLinkResolveClient());
        _ = ctx.Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());
        return ctx;
    }

    [Fact]
    public void SingleMode_RendersExactlyOneRow_AndNoRemoveButton()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, false)
            .Add(x => x.Values, ["123"]));

        cut.FindAll("input").Count.ShouldBe(1);
        cut.FindAll("button").Count.ShouldBe(0);
    }

    [Fact]
    public void MultipleMode_EmptyValues_RendersOneEmptyRow()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, []));

        cut.FindAll("input").Count.ShouldBe(1);
        cut.FindAll("button").Count.ShouldBe(0);
    }

    [Fact]
    public void MultipleMode_FillingLastRow_AddsNewEmptyRow()
    {
        using var ctx = CreateContext();
        List<string> captured = ["a"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("input").Count.ShouldBe(2);

        cut.FindAll("input")[1].Input("b");

        captured.ShouldBe(["a", "b"]);
        cut.FindAll("input").Count.ShouldBe(3);
        cut.FindAll("button").Count.ShouldBe(2);
    }

    [Fact]
    public void MultipleMode_ClearingLastRealRow_CollapsesToVirtualEmptyRow()
    {
        using var ctx = CreateContext();
        List<string> captured = ["a", "b"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("input")[1].Input("");

        captured.ShouldBe(["a"]);
        cut.FindAll("input").Count.ShouldBe(2);
    }

    [Fact]
    public void MultipleMode_RemovingRow_DoesNotLeaveStaleValueInReusedRow()
    {
        using var ctx = CreateContext();
        List<string> captured = ["a", "b"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("button")[1].Click();

        captured.ShouldBe(["a"]);
        cut.FindAll("input").Count.ShouldBe(2);
        cut.Markup.ShouldNotContain("value=\"b\"");
    }

    [Fact]
    public void MultipleMode_TypingWithoutBlur_AddsNewEmptyRowImmediately()
    {
        using var ctx = CreateContext();
        List<string> captured = ["a"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("input")[1].Input("b");

        cut.FindAll("input").Count.ShouldBe(3);
    }

    [Fact]
    public void MultipleMode_RemoveButton_RemovesRowFromMiddle()
    {
        using var ctx = CreateContext();
        List<string> captured = ["a", "b", "c"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("button")[0].Click();

        captured.ShouldBe(["b", "c"]);
        cut.FindAll("input").Count.ShouldBe(3);
        cut.FindAll("button").Count.ShouldBe(2);
    }

    /// <summary>
    /// Если строку не удалось разрезолвить, пользователь видит почему, а не молчание.
    /// </summary>
    [Fact]
    public void UnresolvedValue_ShowsError()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Values, ["кто-то"])
            .Add(x => x.ProjectId, DemoProjectId));

        cut.Markup.ShouldContain("не найден");
    }

    /// <summary>
    /// После ввода строки мастер видит, кого он вписал, а не голый id.
    /// </summary>
    [Fact]
    public void TypingLink_ShowsResolvedUserName()
    {
        using var ctx = CreateContext(new FakeUserLinkResolveClient { Users = { ["123"] = (123, "Вася Пупкин") } });
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Values, [])
            .Add(x => x.ProjectId, DemoProjectId));

        cut.Find("input[type=text]").Change("123");

        cut.Markup.ShouldContain("Вася Пупкин");
    }

    /// <summary>
    /// Без <c>ProjectId</c> резолв выключен совсем — так компонент работает в форме приглашения
    /// игрока, где строка уходит на сервер как есть.
    /// </summary>
    [Fact]
    public void WithoutProjectId_NoResolveCalls()
    {
        var client = new FakeUserLinkResolveClient();
        using var ctx = CreateContext(client);
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Values, ["123"]));

        cut.FindAll("input[type=text]")[0].Change("456");

        client.CallCount.ShouldBe(0);
    }

    private sealed class FakeUserLinkResolveClient : IUserLinkResolveClient
    {
        public Dictionary<string, (int UserId, string Name)> Users { get; } = [];

        public int CallCount { get; private set; }

        public Task<UserLinkViewModel> ResolveUserLink(int projectId, string userLink)
        {
            CallCount++;
            return Users.TryGetValue(userLink, out var user)
                ? Task.FromResult(new UserLinkViewModel(new UserIdentification(user.UserId), user.Name, ViewMode.Show))
                : throw new InvalidOperationException($"Пользователь «{userLink}» не найден.");
        }
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.com/user/{target.UserId?.Value}");
    }
}
