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

    [Fact]
    public void MasterOnly_CheckboxVisible_WhenRegularPlot()
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

        cut.Markup.ShouldContain("Мастерский текст");
    }

    [Fact]
    public void MasterOnly_CheckboxHidden_WhenHandout()
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

        cut.Markup.ShouldNotContain("Мастерский текст");
    }

    [Fact]
    public void MasterOnly_HidesSelectors_WhenChecked()
    {
        using var ctx = CreateContext();
        var model = new PlotElementCreateViewModel
        {
            ProjectId = 1,
            ElementType = PlotElementTypeView.RegularPlot,
            HasPlotEditAccess = true,
            IsMasterOnly = true,
            Content = PlotElementCreateViewModel.GetDefaultContent(),
        };

        var cut = ctx.Render<PlotElementEditPanel>(p =>
            p.Add(x => x.Model, model));

        cut.Markup.ShouldNotContain("Привязка к персонажам");
        cut.Markup.ShouldNotContain("Привязка к группам");
    }

    [Fact]
    public void MasterOnly_ShowsSelectors_WhenUnchecked()
    {
        using var ctx = CreateContext();
        var model = new PlotElementCreateViewModel
        {
            ProjectId = 1,
            ElementType = PlotElementTypeView.RegularPlot,
            HasPlotEditAccess = true,
            IsMasterOnly = false,
            Content = PlotElementCreateViewModel.GetDefaultContent(),
        };

        var cut = ctx.Render<PlotElementEditPanel>(p =>
            p.Add(x => x.Model, model));

        cut.Markup.ShouldContain("Привязка к персонажам");
        cut.Markup.ShouldContain("Привязка к группам");
    }

}
