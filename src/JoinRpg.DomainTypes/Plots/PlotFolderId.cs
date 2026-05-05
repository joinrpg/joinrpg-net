using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.Plots;

[method: JsonConstructor]
[TypedEntityId]
public partial record PlotFolderIdentification(ProjectIdentification ProjectId, int PlotFolderId) : IProjectEntityId
{
    public static implicit operator int(PlotFolderIdentification self) => self.PlotFolderId;
}
