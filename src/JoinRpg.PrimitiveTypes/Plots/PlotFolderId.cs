using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;
[method: JsonConstructor]
public record PlotFolderIdentification(ProjectIdentification ProjectId, int PlotFolderId) : IProjectEntityId
{
    public PlotFolderIdentification(int ProjectId, int PlotFolderId) : this(new ProjectIdentification(ProjectId), PlotFolderId)
    {

    }
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
