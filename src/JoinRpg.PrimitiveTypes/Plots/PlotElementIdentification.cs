using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "PlotElement")]
public partial record class PlotElementIdentification(PlotFolderIdentification PlotFolderId, int PlotElementId)
    : ISpanParsable<PlotElementIdentification>, IProjectEntityId, IComparable<PlotElementIdentification>
{
    public PlotElementIdentification(int projectId, int plotFolderId, int plotElementId) : this(new PlotFolderIdentification(new ProjectIdentification(projectId), plotFolderId), plotElementId)
    {

    }

    public PlotElementIdentification(ProjectIdentification projectId, int plotFolderId, int plotElementId) : this(projectId.Value, plotFolderId, plotElementId)
    {

    }

    public static PlotElementIdentification? FromOptional(PlotFolderIdentification? plotFolderId, int? plotElementId)
        => plotElementId is not null && plotFolderId is not null ? new PlotElementIdentification(plotFolderId, plotElementId.Value) : null;
}
