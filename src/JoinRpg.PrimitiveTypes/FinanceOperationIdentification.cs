using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
public record class FinanceOperationIdentification(ProjectIdentification ProjectId, int ClaimId, int FinanceOperationId) : IProjectEntityId, ISpanParsable<FinanceOperationIdentification>
{
    int IProjectEntityId.Id => FinanceOperationId;

    public FinanceOperationIdentification(int ProjectId, int ClaimId, int FinanceOperationId) : this(new(ProjectId), ClaimId, FinanceOperationId)
    {

    }

    public override string ToString() => $"FinanceOperation({ProjectId.Value}, {ClaimId}, {FinanceOperationId})";

    public static FinanceOperationIdentification? FromOptional(ProjectIdentification? ProjectId, int? ClaimId, int? FinanceOperationId)
    => ProjectId is not null && ClaimId is not null && FinanceOperationId is not null ? new FinanceOperationIdentification(ProjectId, ClaimId.Value, FinanceOperationId.Value) : null;

    public static FinanceOperationIdentification Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
    public static FinanceOperationIdentification Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out FinanceOperationIdentification result)
        => TryParse(s.AsSpan(), provider, out result);

    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [MaybeNullWhen(false)] out FinanceOperationIdentification result)
    {
        var parsed = IdentificationParseHelper.TryParse3(value, provider, [nameof(FinanceOperationIdentification), "FinanceOperation"]);

        if (parsed != null)
        {
            result = new FinanceOperationIdentification(parsed.Value.i1, parsed.Value.i2, parsed.Value.i3);
            return true;
        }

        result = null;
        return false;
    }
}
