using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Сворачивает и раскрывает панель браузер (нативный &lt;details&gt;), поэтому тест проверяет то,
/// на что браузер опирается: заголовок — это summary, аккордеон — общий name, состояние — open,
/// а ссылки и кнопки заголовка (HeaderExtra) лежат вне summary.
/// </summary>
public class JoinCollapsePanelTest
{
    private static IRenderedComponent<JoinCollapsePanel> Render(
        BunitContext ctx,
        Action<ComponentParameterCollectionBuilder<JoinCollapsePanel>>? configure = null)
        => ctx.Render<JoinCollapsePanel>(p =>
        {
            p.Add(x => x.Header, "<b>заголовок</b>");
            p.Add(x => x.HeaderExtra, "<a href=\"/x\">ссылка</a>");
            p.Add(x => x.ChildContent, "<p>содержимое</p>");
            configure?.Invoke(p);
        });

    [Fact]
    public void Header_IsTheSummaryOfDetails()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        cut.Find("details > summary b").TextContent.ShouldBe("заголовок");
    }

    [Fact]
    public void HeaderExtra_IsOutsideSummary_SoLinksDoNotToggle()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        cut.FindAll("summary a").ShouldBeEmpty();
        cut.Find(".panel-heading > a").TextContent.ShouldBe("ссылка");
    }

    [Fact]
    public void Summary_ControlsBody()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, p => p.Add(x => x.Id, "panelPlotElement42"));

        var bodyId = cut.Find("summary").GetAttribute("aria-controls");
        cut.Find($"#{bodyId} p").TextContent.ShouldBe("содержимое");
    }

    [Fact]
    public void ClosedByDefault()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        cut.Find("details").HasAttribute("open").ShouldBeFalse();
    }

    [Fact]
    public void Open_RendersOpen()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, p => p.Add(x => x.Open, true));

        cut.Find("details").HasAttribute("open").ShouldBeTrue();
    }

    [Fact]
    public void WithoutGroup_PanelIsNotPartOfAnyAccordion()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        cut.Find("details").HasAttribute("name").ShouldBeFalse();
    }

    [Fact]
    public void PanelsInGroup_FormOneAccordion_SeparateFromOtherGroup()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<GroupsHost>();

        var names = cut.FindAll("details").Select(d => d.GetAttribute("name")).ToList();

        names.ShouldAllBe(name => !string.IsNullOrEmpty(name));
        names[0].ShouldBe(names[1]);
        names[2].ShouldBe(names[3]);
        names[0].ShouldNotBe(names[2]);
    }

    [Fact]
    public void ExplicitGroup_WinsOverEnclosingGroup()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinCollapsePanelGroup>(p => p
            .AddChildContent<JoinCollapsePanel>(panel => panel
                .Add(x => x.Header, "заголовок")
                .Add(x => x.ChildContent, "содержимое")
                .Add(x => x.Group, "своя")));

        cut.Find("details").GetAttribute("name").ShouldBe("своя");
    }

    /// <summary>Две группы по две панели — проверить, что аккордеоны не смешиваются.</summary>
    private sealed class GroupsHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            for (var group = 0; group < 2; group++)
            {
                builder.OpenComponent<JoinCollapsePanelGroup>(0);
                builder.AddComponentParameter(1, nameof(JoinCollapsePanelGroup.ChildContent), (RenderFragment)(panels =>
                {
                    for (var i = 0; i < 2; i++)
                    {
                        panels.OpenComponent<JoinCollapsePanel>(0);
                        panels.AddComponentParameter(1, nameof(JoinCollapsePanel.Header), (RenderFragment)(h => h.AddContent(0, "заголовок")));
                        panels.AddComponentParameter(2, nameof(JoinCollapsePanel.ChildContent), (RenderFragment)(c => c.AddContent(0, "содержимое")));
                        panels.CloseComponent();
                    }
                }));
                builder.CloseComponent();
            }
        }
    }

    [Fact]
    public void Id_IsOnDetails_SoAnchorCanOpenIt()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, p => p.Add(x => x.Id, "panelPlotElement42"));

        cut.Find("details").Id.ShouldBe("panelPlotElement42");
    }
}
