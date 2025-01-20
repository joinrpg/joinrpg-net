namespace JoinRpg.PrimitiveTypes.Plots;

public record class PlotElementIdentification(PlotFolderIdentification PlotFolderId, int PlotElementId)
{
    public ProjectIdentification ProjectId => PlotFolderId.ProjectId;

    public PlotElementIdentification(int projectId, int plotFolderId, int plotElementId) : this(new PlotFolderIdentification(new ProjectIdentification(projectId), plotFolderId), plotElementId)
    {

    }
}
