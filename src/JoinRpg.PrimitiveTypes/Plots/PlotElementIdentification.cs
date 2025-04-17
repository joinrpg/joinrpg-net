using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;

[method: JsonConstructor]
public record class PlotElementIdentification(PlotFolderIdentification PlotFolderId, int PlotElementId) : ISpanParsable<PlotElementIdentification>, IProjectEntityId
{
    public ProjectIdentification ProjectId => PlotFolderId.ProjectId;

    int IProjectEntityId.Id => PlotElementId;

    public PlotElementIdentification(int projectId, int plotFolderId, int plotElementId) : this(new PlotFolderIdentification(new ProjectIdentification(projectId), plotFolderId), plotElementId)
    {

    }

    public PlotElementIdentification(ProjectIdentification projectId, int plotFolderId, int plotElementId) : this(projectId.Value, plotFolderId, plotElementId)
    {

    }

    public static PlotElementIdentification? FromOptional(PlotFolderIdentification? plotFolderId, int? plotElementId)
        => plotElementId is not null && plotFolderId is not null ? new PlotElementIdentification(plotFolderId, plotElementId.Value) : null;

    public override string ToString() => $"PlotElement({ProjectId.Value}-{PlotFolderId.PlotFolderId}-{PlotElementId})";
    public static PlotElementIdentification Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));

    static PlotElementIdentification IParsable<PlotElementIdentification>.Parse(string value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
    static bool IParsable<PlotElementIdentification>.TryParse(string? value, IFormatProvider? provider, [NotNullWhen(true)] out PlotElementIdentification? result)
        => TryParse(value.AsSpan(), provider, out result);

    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [NotNullWhen(true)] out PlotElementIdentification? result)
    {
        var parsed = IdentificationParseHelper.TryParse3(value, provider, [nameof(PlotElementIdentification), "PlotElement"]);

        if (parsed != null)
        {
            result = new PlotElementIdentification(parsed.Value.i1, parsed.Value.i2, parsed.Value.i3);
            return true;
        }

        result = null;
        return false;
    }

}
