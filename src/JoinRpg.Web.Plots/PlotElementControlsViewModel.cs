namespace JoinRpg.Web.Plots;

public record PlotElementControlsViewModel(
    PlotVersionIdentification? PublishedVersion,
    bool HasEditAccess,
    PlotStatus PlotStatus,
    PlotElementIdentification PlotElementId,
    PlotVersionIdentification CurrentVersion,
    bool CouldBePublished)
{
}
