using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "PlotFolder")]
public partial record PlotFolderIdentification(ProjectIdentification ProjectId, int PlotFolderId)
{
    public static implicit operator int(PlotFolderIdentification self) => self.PlotFolderId;
}
