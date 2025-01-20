namespace JoinRpg.PrimitiveTypes.Plots;
public record class PlotVersionIdentification(PlotElementIdentification PlotElementId, int? Version)
{
    public ProjectIdentification ProjectId => PlotElementId.PlotFolderId.ProjectId;
    public PlotFolderIdentification PlotFolderId => PlotElementId.PlotFolderId;
}
