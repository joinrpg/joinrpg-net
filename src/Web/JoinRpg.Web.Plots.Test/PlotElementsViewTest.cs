using Bunit.TestDoubles;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.Plots.Elements;

namespace JoinRpg.Web.Plots.Test;

/// <summary>Вводная-сетка рендерится компонентом сетки, а не как пустой markdown (ADR029).</summary>
public class PlotElementsViewTest
{
    [Fact]
    public void RoleGrid_RendersGridComponentAndName_InsteadOfContent()
    {
        using var ctx = new BunitContext();
        ctx.ComponentFactories.AddStub<ProjectRoleGrid>();

        var projectId = new ProjectIdentification(1);
        var rolesListId = new ProjectRolesListIdentification(projectId, 30);
        var plot = new PlotRenderedTextViewModel(
            Content: "Этот текст не должен отображаться",
            ShortContent: "***",
            Todo: null,
            new PlotVersionIdentification(new PlotElementIdentification(projectId, 10, 20), 0),
            PlotStatus.Completed,
            Target: null,
            PlotElementTypeView.RoleGrid,
            rolesListId,
            "Соседи по сюжету");

        var cut = ctx.Render<PlotElementsView>(p => p
            .Add(x => x.CharacterId, new CharacterIdentification(projectId, 40))
            .Add(x => x.PlotTexts, [plot])
            .Add(x => x.ShowEditControls, false));

        cut.Markup.ShouldContain("Сетка ролей: Соседи по сюжету");
        cut.Markup.ShouldNotContain("Этот текст не должен отображаться");
        var grid = cut.FindComponent<Stub<ProjectRoleGrid>>();
        grid.Instance.Parameters.Get(x => x.AccessViaPlotElement)
            .ShouldBe(plot.PlotVersionId.PlotElementId);
    }

    [Fact]
    public void RoleGrid_WithDeletedRolesList_RendersFallbackCaption()
    {
        using var ctx = new BunitContext();
        ctx.ComponentFactories.AddStub<ProjectRoleGrid>();

        var projectId = new ProjectIdentification(1);
        var plot = new PlotRenderedTextViewModel(
            Content: "",
            ShortContent: "***",
            Todo: null,
            new PlotVersionIdentification(new PlotElementIdentification(projectId, 10, 20), 0),
            PlotStatus.Completed,
            Target: null,
            PlotElementTypeView.RoleGrid,
            RolesListId: null,
            RolesListName: null);

        var cut = ctx.Render<PlotElementsView>(p => p
            .Add(x => x.CharacterId, new CharacterIdentification(projectId, 40))
            .Add(x => x.PlotTexts, [plot])
            .Add(x => x.ShowEditControls, false));

        cut.Markup.ShouldContain("Сетка ролей: сетка удалена");
        cut.FindComponent<Stub<ProjectRoleGrid>>();
    }

    [Fact]
    public void RegularPlot_RendersMarkdownContent()
    {
        using var ctx = new BunitContext();

        var projectId = new ProjectIdentification(1);
        var plot = new PlotRenderedTextViewModel(
            Content: "<p>Текст обычной вводной</p>",
            ShortContent: "***",
            Todo: null,
            new PlotVersionIdentification(new PlotElementIdentification(projectId, 10, 20), 0),
            PlotStatus.Completed,
            Target: null,
            PlotElementTypeView.RegularPlot,
            RolesListId: null,
            RolesListName: null);

        var cut = ctx.Render<PlotElementsView>(p => p
            .Add(x => x.CharacterId, new CharacterIdentification(projectId, 40))
            .Add(x => x.PlotTexts, [plot])
            .Add(x => x.ShowEditControls, false));

        cut.Markup.ShouldContain("Текст обычной вводной");
    }
}
