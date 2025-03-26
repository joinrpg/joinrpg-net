using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.PrimitiveTypes;

public record ProjectIdentification(int Value) : SingleValueType<int>(Value), ILinkable, ISpanParsable<ProjectIdentification>
{
    LinkType ILinkable.LinkType => LinkType.Project;

    string ILinkable.Identification => Value.ToString();

    int? ILinkable.ProjectId => Value;

    public override string ToString() => $"Project({Value})";

    public static ProjectIdentification? FromOptional(int? value) => value is int id ? new(id) : null;
    public static ProjectIdentification Parse(string value, IFormatProvider? provider) => Parse(value.AsSpan(), provider);

    public static bool TryParse(string? value, IFormatProvider? provider, [MaybeNullWhen(false)] out ProjectIdentification result) => TryParse(value.AsSpan(), provider, out result);

    public static ProjectIdentification Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [MaybeNullWhen(false)] out ProjectIdentification result)
    {
        if (IdentificationParseHelper.TryParse1(value, provider, [nameof(ProjectIdentification), "Project"]) is int id)
        {
            result = new ProjectIdentification(id);
            return true;
        }
        result = new ProjectIdentification(0);
        return false;
    }
}
