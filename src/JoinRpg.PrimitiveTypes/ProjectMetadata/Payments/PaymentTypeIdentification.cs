using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

[method: JsonConstructor]
public record PaymentTypeIdentification(
    ProjectIdentification ProjectId,
    int PaymentTypeId) : IProjectEntityId, IComparable<PaymentTypeIdentification>, ISpanParsable<PaymentTypeIdentification>
{
    int IProjectEntityId.Id => PaymentTypeId;

    public PaymentTypeIdentification(int projectId, int paymentTypeId) : this(new(projectId), paymentTypeId) { }

    public static PaymentTypeIdentification? FromOptional(int ProjectId, int? paymentTypeId)
    {
        if (paymentTypeId is null || paymentTypeId == -1)
        {
            return null;
        }
        else
        {
            return new PaymentTypeIdentification(ProjectId, paymentTypeId.Value);
        }
    }

    public static IEnumerable<PaymentTypeIdentification> FromList(IEnumerable<int> list, ProjectIdentification projectId) => list.Select(g => new PaymentTypeIdentification(projectId, g));

    int IComparable<PaymentTypeIdentification>.CompareTo(PaymentTypeIdentification? other) => Comparer<int>.Default.Compare(PaymentTypeId, other?.PaymentTypeId ?? -1);

    public override string ToString() => $"PaymentType({ProjectId}-{PaymentTypeId})";

    public static PaymentTypeIdentification Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TryParse(s, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(s));

    public static PaymentTypeIdentification Parse(string s, IFormatProvider? provider) => TryParse(s.AsSpan(), provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(s));
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out PaymentTypeIdentification result) => TryParse(s.AsSpan(), provider, out result);

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out PaymentTypeIdentification result)
    {
        var parsed = IdentificationParseHelper.TryParse2(s, provider, [nameof(PaymentTypeIdentification), "PaymentType"]);

        if (parsed != null)
        {
            result = new PaymentTypeIdentification(parsed.Value.i1, parsed.Value.i2);
            return true;
        }

        result = null;
        return false;
    }
}
