using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.PrimitiveTypes.Plots;

public record class PlotElementIdentification(PlotFolderIdentification PlotFolderId, int PlotElementId) : ISpanParsable<PlotElementIdentification>
{
    public ProjectIdentification ProjectId => PlotFolderId.ProjectId;

    public PlotElementIdentification(int projectId, int plotFolderId, int plotElementId) : this(new PlotFolderIdentification(new ProjectIdentification(projectId), plotFolderId), plotElementId)
    {

    }

    public override string ToString() => $"PlotElement({ProjectId.Value}-{PlotFolderId.PlotFolderId}-{PlotElementId})";
    static PlotElementIdentification ISpanParsable<PlotElementIdentification>.Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));

    static PlotElementIdentification IParsable<PlotElementIdentification>.Parse(string value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
    static bool IParsable<PlotElementIdentification>.TryParse(string? value, IFormatProvider? provider, [NotNullWhen(true)] out PlotElementIdentification? result)
        => TryParse(value.AsSpan(), provider, out result);

    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [NotNullWhen(true)] out PlotElementIdentification? result)
    {
        var val = value.Trim();
        if (val.StartsWith(nameof(PlotElementIdentification)))
        {
            val = val[nameof(PlotElementIdentification).Length..];
        }
        if (val.StartsWith("PlotElement"))
        {
            val = val["PlotElement".Length..];
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

        Span<Range> ranges = stackalloc Range[3];
        var count = val.Split(ranges, "-", StringSplitOptions.TrimEntries);
        if (count == 3
           && ProjectIdentification.TryParse(val[ranges[0]], provider, out var projectId)
           && int.TryParse(val[ranges[1]], provider, out var plotfolderInt)
           && int.TryParse(val[ranges[2]], provider, out var plotElementInt)
           )
        {
            result = new PlotElementIdentification(projectId, plotfolderInt, plotElementInt);
            return true;
        }

        result = null;
        return false;
    }

}
