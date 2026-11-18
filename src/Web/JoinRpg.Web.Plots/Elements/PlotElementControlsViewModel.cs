namespace JoinRpg.Web.Plots.Elements;

public record PlotElementControlsViewModel(
    PlotVersionIdentification? PublishedVersion,
    bool HasEditAccess,
    PlotStatus PlotStatus,
    PlotElementIdentification PlotElementId,
    PlotVersionIdentification CurrentVersion,
    bool CouldBePublished)
{
}
