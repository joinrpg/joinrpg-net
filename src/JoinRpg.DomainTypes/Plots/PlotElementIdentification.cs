using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.Plots;

[method: JsonConstructor]
[TypedEntityId]
public partial record class PlotElementIdentification(PlotFolderIdentification PlotFolderId, int PlotElementId) : IProjectEntityId
{
}
