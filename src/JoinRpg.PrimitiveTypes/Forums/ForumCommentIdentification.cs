using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
public record class ForumCommentIdentification(ForumThreadIdentification ThreadId, int CommentId)
    : ISpanParsable<ForumCommentIdentification>, IProjectEntityId, IComparable<ForumCommentIdentification>
{
    public ProjectIdentification ProjectId => ThreadId.ProjectId;

    int IProjectEntityId.Id => CommentId;

    public ForumCommentIdentification(int projectId, int threadId, int commentId)
        : this(new ForumThreadIdentification(new ProjectIdentification(projectId), threadId), commentId)
    {

    }

    public ForumCommentIdentification(ProjectIdentification projectId, int claimId, int commentId) : this(projectId.Value, claimId, commentId)
    {

    }

    public static ForumCommentIdentification? FromOptional(ForumThreadIdentification? threadId, int? commentId)
        => commentId is not null && threadId is not null ? new ForumCommentIdentification(threadId, commentId.Value) : null;

    public override string ToString() => $"ForumComment({ProjectId.Value}-{ThreadId.ThreadId}-{CommentId})";
    public static ForumCommentIdentification Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));

    static ForumCommentIdentification IParsable<ForumCommentIdentification>.Parse(string value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
    static bool IParsable<ForumCommentIdentification>.TryParse(string? value, IFormatProvider? provider, [NotNullWhen(true)] out ForumCommentIdentification? result)
        => TryParse(value.AsSpan(), provider, out result);

    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [NotNullWhen(true)] out ForumCommentIdentification? result)
    {
        var parsed = IdentificationParseHelper.TryParse3(value, provider, [nameof(ForumCommentIdentification), "ForumComment"]);

        if (parsed != null)
        {
            result = new ForumCommentIdentification(parsed.Value.i1, parsed.Value.i2, parsed.Value.i3);
            return true;
        }

        result = null;
        return false;
    }

    public int CompareTo(ForumCommentIdentification? other) => Comparer<int>.Default.Compare(CommentId, other?.CommentId ?? -1);

}
