using JoinRpg.Web.Plots;
using JoinRpg.Web.ProjectCommon;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Plots.Tests;

public class PlotElementEditPanelTest
{
    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        ctx.ComponentFactories.AddStub<PlotFolderSelector>();
        ctx.ComponentFactories.AddStub<CharacterSelector>();
        ctx.ComponentFactories.AddStub<CharacterGroupSelector>();
        return ctx;
    }

    [Fact]
    public void SwitchToHandout_ShouldShowHandoutContent()
    {
        using var ctx = CreateContext();
        var model = new PlotElementCreateViewModel
        {
            ProjectId = 1,
            ElementType = PlotElementTypeView.RegularPlot,
            HasPlotEditAccess = true,
            Content = PlotElementCreateViewModel.GetDefaultContent(),
        };

        var cut = ctx.Render<PlotElementEditPanel>(p =>
            p.Add(x => x.Model, model));

        cut.Markup.ShouldContain("Текст вводной");
        cut.Markup.ShouldNotContain("Что выдать");

        cut.Find($"input[type=radio][value='{PlotElementTypeView.Handout}']")
           .Change(PlotElementTypeView.Handout.ToString());

        cut.Markup.ShouldContain("Что выдать");
        cut.Markup.ShouldNotContain("Текст вводной");
    }

    [Fact]
    public void SwitchBackToRegularPlot_ShouldShowTextareaContent()
    {
        using var ctx = CreateContext();
        var model = new PlotElementCreateViewModel
        {
            ProjectId = 1,
            ElementType = PlotElementTypeView.Handout,
            HasPlotEditAccess = true,
            Content = "test",
        };

        var cut = ctx.Render<PlotElementEditPanel>(p =>
            p.Add(x => x.Model, model));

        cut.Markup.ShouldContain("Что выдать");

        cut.Find($"input[type=radio][value='{PlotElementTypeView.RegularPlot}']")
           .Change(PlotElementTypeView.RegularPlot.ToString());

        cut.Markup.ShouldContain("Текст вводной");
        cut.Markup.ShouldNotContain("Что выдать");
    }
}
