using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "PlotFolder")]
public partial record PlotFolderIdentification(ProjectIdentification ProjectId, int PlotFolderId) :
    IProjectEntityId, ISpanParsable<PlotFolderIdentification>, IComparable<PlotFolderIdentification>
{
    public PlotFolderIdentification(int ProjectId, int PlotFolderId) : this(new ProjectIdentification(ProjectId), PlotFolderId)
    {

    }

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
}
