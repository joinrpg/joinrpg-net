using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "PlotElement")]
public partial record class PlotElementIdentification(PlotFolderIdentification PlotFolderId, int PlotElementId)
{
}
