namespace JoinRpg.PrimitiveTypes.Plots;
public record PlotFolderIdentification(ProjectIdentification ProjectId, int PlotFolderId)
{
    public static PlotFolderIdentification? FromOptional(ProjectIdentification? project, int? plotFolderId)
    {
        return (project, plotFolderId) switch
        {
            (_, null) => null,
            (null, _) => null,
            _ => new(project, plotFolderId.Value)
        };
    }

    public override string ToString() => $"PlotFolderId({PlotFolderId}, {ProjectId})";
}
