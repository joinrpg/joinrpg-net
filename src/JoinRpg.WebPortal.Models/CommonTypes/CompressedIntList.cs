using System.Diagnostics.CodeAnalysis;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.CommonTypes;
public record class CompressedIntList(IReadOnlyCollection<int> List) : IParsable<CompressedIntList>
{
    public static CompressedIntList Parse(string s, IFormatProvider? provider) => TryParse(s, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(s));

    public static bool TryParse(string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out CompressedIntList result)
    {
        if (s == null)
        {
            result = null;
            return false;
        }
        result = new CompressedIntList(s.UnCompressIdListImpl());
        return true;
    }

    public IReadOnlyCollection<CharacterIdentification> ToCharacterIds(ProjectIdentification projectId)
    {
        return [.. List.Select(x => new CharacterIdentification(projectId, x))];
    }
}
