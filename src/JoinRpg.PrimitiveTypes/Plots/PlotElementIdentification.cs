using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
[ProjectEntityId(AdditionalPrefixes = ["PlotElement"])]
public partial record class PlotElementIdentification(PlotFolderIdentification PlotFolderId, int PlotElementId)
{
}
