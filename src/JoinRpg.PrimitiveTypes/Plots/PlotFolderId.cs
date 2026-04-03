using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
[TypedEntityId]
public partial record struct PlotFolderIdentification(ProjectIdentification ProjectId, int PlotFolderId) : IProjectEntityId
{
    public static implicit operator int(PlotFolderIdentification self) => self.PlotFolderId;
}
