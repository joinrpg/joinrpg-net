using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Claims;

[method: JsonConstructor]
public record class ClaimCommentIdentification(ClaimIdentification ClaimId, int CommentId)
    : ISpanParsable<ClaimCommentIdentification>, IProjectEntityId, IComparable<ClaimCommentIdentification>
{
    public ProjectIdentification ProjectId => ClaimId.ProjectId;

    int IProjectEntityId.Id => CommentId;

    public ClaimCommentIdentification(int projectId, int claimId, int commentId)
        : this(new ClaimIdentification(new ProjectIdentification(projectId), claimId), commentId)
    {

    }

    public ClaimCommentIdentification(ProjectIdentification projectId, int claimId, int commentId) : this(projectId.Value, claimId, commentId)
    {

    }

    public static ClaimCommentIdentification? FromOptional(ClaimIdentification? claimId, int? commentId)
        => commentId is not null && claimId is not null ? new ClaimCommentIdentification(claimId, commentId.Value) : null;

    public override string ToString() => $"ClaimComment({ProjectId.Value}-{ClaimId.ClaimId}-{CommentId})";
    public static ClaimCommentIdentification Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));

    static ClaimCommentIdentification IParsable<ClaimCommentIdentification>.Parse(string value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
    static bool IParsable<ClaimCommentIdentification>.TryParse(string? value, IFormatProvider? provider, [NotNullWhen(true)] out ClaimCommentIdentification? result)
        => TryParse(value.AsSpan(), provider, out result);

    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [NotNullWhen(true)] out ClaimCommentIdentification? result)
    {
        var parsed = IdentificationParseHelper.TryParse3(value, provider, [nameof(ClaimCommentIdentification), "ClaimComment"]);

        if (parsed != null)
        {
            result = new ClaimCommentIdentification(parsed.Value.i1, parsed.Value.i2, parsed.Value.i3);
            return true;
        }

        result = null;
        return false;
    }

    public int CompareTo(ClaimCommentIdentification? other) => Comparer<int>.Default.Compare(CommentId, other?.CommentId ?? -1);

}
