using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
[TypedEntityId]
public partial record class PlotElementIdentification(PlotFolderIdentification PlotFolderId, int PlotElementId) : IProjectEntityId
{
}
