using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Plots;
[method: JsonConstructor]
public record PlotFolderIdentification(ProjectIdentification ProjectId, int PlotFolderId) : IProjectEntityId, ISpanParsable<PlotFolderIdentification>
{
    public PlotFolderIdentification(int ProjectId, int PlotFolderId) : this(new ProjectIdentification(ProjectId), PlotFolderId)
    {

    }
    int IProjectEntityId.Id => PlotFolderId;

    public static implicit operator int(PlotFolderIdentification self) => self.PlotFolderId;

    public static PlotFolderIdentification? FromOptional(ProjectIdentification? project, int? plotFolderId)
    {
        return (project, plotFolderId) switch
        {
            (_, null) => null,
            (null, _) => null,
            _ => new(project, plotFolderId.Value)
        };
    }

    public override string ToString() => $"PlotFolder({PlotFolderId}, {ProjectId})";
    public static PlotFolderIdentification Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TryParse(s, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(s));

    public static PlotFolderIdentification Parse(string s, IFormatProvider? provider) => TryParse(s.AsSpan(), provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(s));
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out PlotFolderIdentification result) => TryParse(s.AsSpan(), provider, out result);

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out PlotFolderIdentification result)
    {
        var parsed = IdentificationParseHelper.TryParse2(s, provider, [nameof(PlotFolderIdentification), "PlotFolder"]);

        if (parsed != null)
        {
            result = new PlotFolderIdentification(parsed.Value.i1, parsed.Value.i2);
            return true;
        }

        result = null;
        return false;
    }
}
