using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "PlotVersion", SkipComparable = true)]
public partial record class PlotVersionIdentification(PlotElementIdentification PlotElementId, int Version)
    : IParsable<PlotVersionIdentification>, ISpanParsable<PlotVersionIdentification>, IProjectEntityId, IComparable<PlotVersionIdentification>
{
    public PlotVersionIdentification(int projectId, int plotFolderId, int plotElementId, int version)
        : this(new PlotElementIdentification(new PlotFolderIdentification(new ProjectIdentification(projectId), plotFolderId), plotElementId), version)
    {

    }

    public PlotVersionIdentification(ProjectIdentification projectId, int plotFolderId, int plotElementId, int version)
        : this(new PlotElementIdentification(new PlotFolderIdentification(projectId, plotFolderId), plotElementId), version)
    {

    }

    public PlotFolderIdentification PlotFolderId => PlotElementId.PlotFolderId;

    public static PlotVersionIdentification? FromOptional(PlotElementIdentification? plotElementId, int? Version)
        => Version is not null && plotElementId is not null ? new PlotVersionIdentification(plotElementId, Version.Value) : null;

    public PlotVersionIdentification Next() => this with { Version = Version + 1 };
    public PlotVersionIdentification? Prev() => Version == 0 ? null : this with { Version = Version - 1 };

    int IComparable<PlotVersionIdentification>.CompareTo(PlotVersionIdentification? other)
    {
        if (PlotElementId == other?.PlotElementId)
        {
            return Comparer<int>.Default.Compare(Version, other.Version);
        }
        return PlotElementId.CompareTo(other?.PlotElementId);
    }
}
