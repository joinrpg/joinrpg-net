using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Размер кнопок <see cref="JoinMoveControl{TItem}"/> без явного <see cref="JoinMoveControl{TItem}.Size"/>
/// берётся из группы, в которой они стоят (#5305). Ошибку перемещения контрол показывает сам (#5735).
/// </summary>
public class JoinMoveControlTest
{
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

    private static readonly IList<Item> Items = [new("1"), new("2"), new("3")];

    private static BunitContext CreateContext(IMoveClient? moveClient = null)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        ctx.Services.AddSingleton(moveClient ?? new NoMoveClient());
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        return ctx;
    }

    /// <summary>Классы кнопок перемещения — без кнопок диалога сортировки.</summary>
    private static string[][] MoveButtonClasses(IRenderedComponent<JoinButtonGroup> cut)
        => [.. cut.FindAll("[role=group] > button, [role=group] > .join-tooltip-container button")
            .Select(b => (b.GetAttribute("class") ?? "").Split(' '))];

    private static IRenderedComponent<JoinButtonGroup> RenderInGroup(BunitContext ctx, bool skipGroup, SizeStyleEnum? size)
        => ctx.Render<JoinButtonGroup>(p => p
            .Add(x => x.Size, SizeStyleEnum.Small)
            .AddChildContent<JoinMoveControl<Item>>(m => m
                .Add(x => x.Items, Items)
                .Add(x => x.Self, Items[1])
                .Add(x => x.SkipGroup, skipGroup)
                .Add(x => x.Size, size)));

    private sealed class FailingMoveClient(Exception failure) : IMoveClient
    {
        public Task<string[]> MoveAfterAsync(string selfId, string parentId, string? moveAfterId)
            => Task.FromException<string[]>(failure);
    }

    private static async Task<IRenderedComponent<JoinMoveControl<Item>>> ClickUp(BunitContext ctx)
    {
        var items = new List<Item>(Items);
        var cut = ctx.Render<JoinMoveControl<Item>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.Self, items[1]));
        await cut.Find("button[aria-label='Переместить вверх']").ClickAsync(new MouseEventArgs());
        return cut;
    }

    /// <summary>Отказ сервера (например, проект заблокирован) показывается, а порядок не меняется (#5735).</summary>
    [Fact]
    public async Task ServerRefusal_IsShown()
    {
        using var ctx = CreateContext(new FailingMoveClient(new ServerProblemException("Проект заблокирован на время восстановления данных")));

        var cut = await ClickUp(ctx);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Проект заблокирован на время восстановления данных"));
        cut.Instance.Items.Select(i => i.Id).ShouldBe(["1", "2", "3"]);
    }

    [Fact]
    public async Task UnexpectedFailure_ShowsGenericText()
    {
        using var ctx = CreateContext(new FailingMoveClient(new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).")));

        var cut = await ClickUp(ctx);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось переместить"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    [Fact]
    public void SkipGroup_WithoutOwnSize_TakesOuterGroupSize()
    {
        using var ctx = CreateContext();
        var buttons = MoveButtonClasses(RenderInGroup(ctx, skipGroup: true, size: null));

        buttons.Length.ShouldBe(3);
        buttons.ShouldAllBe(c => c.Contains("join-btn--sm"));
    }

    [Fact]
    public void OwnSize_WinsOverGroup()
    {
        using var ctx = CreateContext();
        var buttons = MoveButtonClasses(RenderInGroup(ctx, skipGroup: true, size: SizeStyleEnum.ExtraSmall));

        buttons.Length.ShouldBe(3);
        buttons.ShouldAllBe(c => c.Contains("join-btn--xs") && !c.Contains("join-btn--sm"));
    }
}
