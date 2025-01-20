namespace JoinRpg.PrimitiveTypes.Plots;
public record PlotFolderIdentification(ProjectIdentification ProjectId, int PlotFolderId) : IProjectEntityId
{
    int IProjectEntityId.Id => PlotFolderId;

    public static implicit operator int(PlotFolderIdentification self) => self.PlotFolderId;

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
