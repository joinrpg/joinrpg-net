using JoinRpg.Common.PrimitiveTypes;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Common.WebComponents.Test;

public class JoinUserLinkEditorTest
{
    /// <summary>
    /// Резолв включён ровно тогда, когда клиент есть в DI, поэтому обычный контекст его регистрирует.
    /// </summary>
    private static BunitContext CreateContext(FakeUserLinkResolveClient? client = null)
    {
        var ctx = CreateContextWithoutResolveClient();
        _ = ctx.Services.AddSingleton<IUserLinkResolveClient>(client ?? new FakeUserLinkResolveClient());
        return ctx;
    }

    private static BunitContext CreateContextWithoutResolveClient()
    {
        var ctx = new BunitContext();
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
    /// Без <c>Name</c> скрытого инпута нет: компонент живёт и внутри Blazor-форм, где значение
    /// уезжает через биндинг, а лишний именованный инпут только мешал бы.
    /// </summary>
    [Fact]
    public void WithoutName_NoHiddenInput()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Values, ["123"]));

        cut.FindAll("input[type=hidden]").Count.ShouldBe(0);
    }

    /// <summary>
    /// Главное требование встраивания в MVC-форму: id уезжает скрытым инпутом с именем поля.
    /// </summary>
    [Fact]
    public void WithName_HiddenInputCarriesResolvedId()
    {
        using var ctx = CreateContext(new FakeUserLinkResolveClient { Users = { ["vk.com/id456"] = (456, "Вася Пупкин") } });
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Values, ["vk.com/id456"])
            .Add(x => x.Name, "field_1"));

        var hidden = cut.Find("input[type=hidden]");
        hidden.GetAttribute("name").ShouldBe("field_1");
        hidden.GetAttribute("value").ShouldBe("456");
    }

    /// <summary>
    /// Неразрезолвленное значение в форму не уезжает — иначе в базу попал бы мусор вместо id.
    /// </summary>
    [Fact]
    public void UnresolvedValue_DoesNotGoToHiddenInput_AndShowsError()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Values, ["кто-то"])
            .Add(x => x.Name, "field_1"));

        cut.Find("input[type=hidden]").GetAttribute("value").ShouldBe("");
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
            .Add(x => x.Name, "field_1"));

        cut.Find("input[type=text]").Change("123");

        cut.Markup.ShouldContain("Вася Пупкин");
        cut.Find("input[type=hidden]").GetAttribute("value").ShouldBe("123");
    }

    /// <summary>
    /// Сохранённое значение — это id; при загрузке страницы он должен показываться именем.
    /// Имя приходит готовым из модели страницы, лишнего запроса за ним нет.
    /// </summary>
    [Fact]
    public void InitialUsers_ShownWithoutResolveCall()
    {
        var client = new FakeUserLinkResolveClient();
        using var ctx = CreateContext(client);
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Values, ["123"])
            .Add(x => x.Name, "field_1")
            .Add(x => x.InitialUsers, [new UserLinkViewModel(new UserIdentification(123), "Вася Пупкин", ViewMode.Show)]));

        cut.Markup.ShouldContain("Вася Пупкин");
        cut.Find("input[type=hidden]").GetAttribute("value").ShouldBe("123");
        client.CallCount.ShouldBe(0);
    }

    /// <summary>
    /// Контракт компонента: без зарегистрированного клиента резолва он остаётся обычным вводом
    /// строк и ничего не роняет — так его использует форма приглашения игрока.
    /// </summary>
    [Fact]
    public void WithoutResolveClient_WorksAsPlainInput()
    {
        using var ctx = CreateContextWithoutResolveClient();
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Values, ["123"]));

        cut.FindAll("input[type=text]")[0].Change("456");

        cut.FindAll("input[type=text]").Count.ShouldBe(1);
        cut.Markup.ShouldNotContain("join-user-link-editor-status");
    }

    private sealed class FakeUserLinkResolveClient : IUserLinkResolveClient
    {
        public Dictionary<string, (int UserId, string Name)> Users { get; } = [];

        public int CallCount { get; private set; }

        public Task<UserLinkViewModel> ResolveUserLink(string userLink)
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
