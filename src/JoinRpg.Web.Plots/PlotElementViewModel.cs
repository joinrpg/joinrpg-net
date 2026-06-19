using Microsoft.AspNetCore.Components;
using static JoinRpg.Web.Plots.PlotStatus;

namespace JoinRpg.Web.Plots;

public record class PlotRenderedTextViewModel(MarkupString Content, string? Todo, PlotVersionIdentification PlotVersionId, PlotStatus? PlotStatus, TargetsInfo? Target)
{
    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(Todo) || PlotStatus == InWork || PlotStatus == HasNewVersion;
}
