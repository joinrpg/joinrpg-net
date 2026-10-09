using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// <see cref="JoinButton.Title"/> — подсказка при наведении (<see cref="Tooltip"/>), а у кнопки из одной иконки
/// ещё и её имя для скринридера: видимого текста у такой кнопки нет, а подсказка скрыта до наведения.
/// </summary>
public class JoinButtonTitleTest
{
    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    [Fact]
    public void IconOnlyButton_TitleIsHintAndAccessibleName()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButton>(p => p
            .Add(x => x.Preset, ButtonPreset.Up)
            .Add(x => x.Title, "Переместить вверх"));

        cut.Find(".join-tooltip").TextContent.ShouldBe("Переместить вверх");
        cut.Find("button").GetAttribute("aria-label").ShouldBe("Переместить вверх");
    }

    [Fact]
    public void IconOnlyLink_TitleIsHintAndAccessibleName()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButton>(p => p
            .Add(x => x.Icon, JoinIconType.Edit)
            .Add(x => x.Link, "/x")
            .Add(x => x.Title, "Изменить персонажа"));

        cut.Find(".join-tooltip").TextContent.ShouldBe("Изменить персонажа");
        cut.Find("a").GetAttribute("aria-label").ShouldBe("Изменить персонажа");
    }

    [Fact]
    public void JoinIconButton_TitleIsHintAndAccessibleName()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinIconButton>(p => p
            .Add(x => x.Preset, ButtonPreset.Edit)
            .Add(x => x.Link, "/x")
            .Add(x => x.Title, "Редактировать персонажа"));

        cut.Find(".join-tooltip").TextContent.ShouldBe("Редактировать персонажа");
        cut.Find("a").GetAttribute("aria-label").ShouldBe("Редактировать персонажа");
    }

    [Fact]
    public void ButtonWithText_TitleIsHint_NameIsVisibleText()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButton>(p => p
            .Add(x => x.Label, "Сохранить")
            .Add(x => x.Title, "Сохранить изменения в поле"));

        cut.Find(".join-tooltip").TextContent.ShouldBe("Сохранить изменения в поле");
        cut.Find("button").TextContent.Trim().ShouldBe("Сохранить");
    }

    [Fact]
    public void JoinMoveControl_ButtonsAreNamedByTheirHints()
    {
        using var ctx = CreateContext();
        ctx.Services.AddSingleton<IMoveClient>(new NoMoveClient());
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        IList<Item> items = [new("1"), new("2"), new("3")];

        var cut = ctx.Render<JoinMoveControl<Item>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.Self, items[1]));

        string[] expected = ["Переместить вверх", "Переместить вниз", "Переместить в любую позицию"];
        cut.FindAll("[role=group] .join-tooltip").Select(t => t.TextContent).ShouldBe(expected);
        cut.FindAll("[role=group] button").Select(b => b.GetAttribute("aria-label")).ShouldBe(expected);
    }

    private sealed record Item(string Id) : IMoveableListItem
    {
        public string ParentId => "parent";
        public string DisplayText => Id;
        public string Subtext => "";
    }

    private sealed class NoMoveClient : IMoveClient
    {
        public Task<string[]> MoveAfterAsync(string selfId, string parentId, string? moveAfterId)
            => throw new NotSupportedException();
    }
}
