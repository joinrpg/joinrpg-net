using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "PlotVersion", SkipComparable = true)]
public partial record class PlotVersionIdentification(PlotElementIdentification PlotElementId, int Version)
    : IComparable<PlotVersionIdentification>
{
    public PlotFolderIdentification PlotFolderId => PlotElementId.PlotFolderId;

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
