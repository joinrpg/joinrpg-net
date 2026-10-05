using JoinRpg.Common.WebComponents.Dialog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Размер группы доходит до кнопок через каскад, а не через правило Bootstrap 3
/// <c>.btn-group-sm &gt; .btn</c>, которое не видит кнопку в обёртке подсказки.
/// </summary>
public class JoinButtonGroupTest
{
    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private static string[] ButtonClasses(IRenderedComponent<JoinButtonGroup> cut, string selector)
        => [.. cut.FindAll(selector).Select(b => b.GetAttribute("class") ?? "")];

    [Fact]
    public void Group_DoesNotUseBootstrapButtonGroup()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButtonGroup>(p => p
            .Add(x => x.Size, SizeStyleEnum.Small)
            .AddChildContent<JoinButton>(b => b.Add(x => x.Label, "Кнопка")));

        var group = cut.Find("[role=group]");
        group.ClassList.ShouldContain("join-btn-group");
        group.ClassList.ShouldNotContain("btn-group");
        group.ClassList.ShouldNotContain("btn-group-sm");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Подсказка")]
    public void SmallGroup_MakesButtonSmall_WithOrWithoutTooltip(string? title)
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButtonGroup>(p => p
            .Add(x => x.Size, SizeStyleEnum.Small)
            .AddChildContent<JoinButton>(b => b
                .Add(x => x.Label, "Кнопка")
                .Add(x => x.Title, title)));

        ButtonClasses(cut, "button").ShouldHaveSingleItem().Split(' ').ShouldContain("join-btn--sm");
    }

    [Fact]
    public void ButtonOwnSize_WinsOverGroup()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButtonGroup>(p => p
            .Add(x => x.Size, SizeStyleEnum.Small)
            .AddChildContent<JoinButton>(b => b
                .Add(x => x.Label, "Кнопка")
                .Add(x => x.Size, SizeStyleEnum.Large)));

        var classes = ButtonClasses(cut, "button").ShouldHaveSingleItem().Split(' ');
        classes.ShouldContain("join-btn--lg");
        classes.ShouldNotContain("join-btn--sm");
    }

    [Fact]
    public void ConfirmationDialogButtons_DoNotInheritGroupSize()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButtonGroup>(p => p
            .Add(x => x.Size, SizeStyleEnum.Small)
            .AddChildContent<JoinButton>(b => b
                .Add(x => x.Label, "Удалить")
                .Add(x => x.AutoConfirm, true)
                .Add(x => x.AutoConfirmMessage, "Точно?")));

        var dialogButtons = ButtonClasses(cut, "dialog .join-dialog-footer button");
        dialogButtons.ShouldNotBeEmpty();
        dialogButtons.ShouldAllBe(c => !c.Split(' ', StringSplitOptions.None).Contains("join-btn--sm"));
    }

    /// <summary>
    /// Диалог в группе бывает и не от AutoConfirm: например, сортировка у JoinMoveControl SkipGroup="true"
    /// в таблице значений поля (FieldValuesListControl) лежит прямо в группе Size=Small.
    /// </summary>
    [Fact]
    public void AnyDialogButtons_DoNotInheritGroupSize()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButtonGroup>(p => p
            .Add(x => x.Size, SizeStyleEnum.Small)
            .AddChildContent<JoinDialog>(d => d
                .Add(x => x.ChildContent, "Содержимое")
                .Add(x => x.Buttons, [new DialogButton(ButtonPreset.Ok), new DialogButton(ButtonPreset.Cancel)])));

        var dialogButtons = ButtonClasses(cut, "dialog .join-dialog-footer button");
        dialogButtons.Length.ShouldBe(2);
        dialogButtons.ShouldAllBe(c => !c.Split(' ', StringSplitOptions.None).Contains("join-btn--sm"));
    }

    [Fact]
    public void SkipGroup_RendersNoWrapper_AndPassesOuterSizeThrough()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButtonGroup>(p => p
            .Add(x => x.Size, SizeStyleEnum.Small)
            .AddChildContent<JoinButtonGroup>(inner => inner
                .Add(x => x.SkipGroup, true)
                .Add(x => x.Size, SizeStyleEnum.Large)
                .AddChildContent<JoinButton>(b => b.Add(x => x.Label, "Кнопка"))));

        cut.FindAll("[role=group]").Count.ShouldBe(1);
        ButtonClasses(cut, "button").ShouldHaveSingleItem().Split(' ').ShouldContain("join-btn--sm");
    }

    [Fact]
    public void ButtonOutsideGroup_HasNoSizeClass()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButton>(b => b.Add(x => x.Label, "Кнопка"));

        cut.Find("button").ClassList.ShouldAllBe(c => c != "join-btn--sm" && c != "join-btn--lg" && c != "join-btn--xs");
    }
}
