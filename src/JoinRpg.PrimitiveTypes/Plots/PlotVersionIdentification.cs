using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.PrimitiveTypes.Plots;
public record class PlotVersionIdentification(PlotElementIdentification PlotElementId, int Version) : IParsable<PlotVersionIdentification>
{
    public PlotVersionIdentification(int projectId, int plotFolderId, int plotElementId, int version)
        : this(new PlotElementIdentification(new PlotFolderIdentification(new ProjectIdentification(projectId), plotFolderId), plotElementId), version)
    {

    }

    public ProjectIdentification ProjectId => PlotElementId.PlotFolderId.ProjectId;
    public PlotFolderIdentification PlotFolderId => PlotElementId.PlotFolderId;

    public static PlotVersionIdentification Parse(string value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
    public static bool TryParse([NotNullWhen(true)] string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out PlotVersionIdentification result)
    {
        var val = value.AsSpan().Trim();
        if (val.StartsWith(nameof(PlotVersionIdentification)))
        {
            val = val[nameof(PlotVersionIdentification).Length..];
        }
        if (val.StartsWith("PlotVersion"))
        {
            val = val["PlotVersion".Length..];
        }
        if (val.StartsWith("("))
        {
            val = val[1..];
        }
        if (val.EndsWith(")"))
        {
            val = val[..^1];
        }
        value = val.ToString();

        Span<Range> ranges = stackalloc Range[4];
        var count = val.Split(ranges, "-", StringSplitOptions.TrimEntries);
        if (count == 4
           && ProjectIdentification.TryParse(val[ranges[0]], provider, out var projectId)
           && int.TryParse(val[ranges[1]], provider, out var plotfolderInt)
           && int.TryParse(val[ranges[2]], provider, out var plotElementInt)
           && int.TryParse(val[ranges[3]], provider, out var plotVersionInt))
        {
            result = new PlotVersionIdentification(projectId, plotfolderInt, plotElementInt, plotVersionInt);
            return true;
        }

        result = null;
        return false;

    }

    public override string ToString() => $"PlotVersion({ProjectId.Value}-{PlotFolderId.PlotFolderId}-{PlotElementId.PlotElementId}-{Version})";
}
