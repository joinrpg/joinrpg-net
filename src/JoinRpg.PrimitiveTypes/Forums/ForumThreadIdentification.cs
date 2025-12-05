using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
public record class ForumThreadIdentification(ProjectIdentification ProjectId, int ThreadId)
    : ISpanParsable<ForumThreadIdentification>, IProjectEntityId, IComparable<ForumThreadIdentification>
{
    int IProjectEntityId.Id => ThreadId;

    public ForumThreadIdentification(int projectId, int threadId) : this(new ProjectIdentification(projectId), threadId) { }

    public static ForumThreadIdentification? FromOptional(ProjectIdentification projectId, int? threadId)
        => threadId is not null ? new ForumThreadIdentification(projectId, threadId.Value) : null;

    public override string ToString() => $"ForumComment({ProjectId.Value}-{ThreadId})";
    public static ForumThreadIdentification Parse(ReadOnlySpan<char> value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));

    static ForumThreadIdentification IParsable<ForumThreadIdentification>.Parse(string value, IFormatProvider? provider)
        => TryParse(value, provider, out var result) ? result : throw new ArgumentException("Could not parse supplied value.", nameof(value));
    static bool IParsable<ForumThreadIdentification>.TryParse(string? value, IFormatProvider? provider, [NotNullWhen(true)] out ForumThreadIdentification? result)
        => TryParse(value.AsSpan(), provider, out result);

    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, [NotNullWhen(true)] out ForumThreadIdentification? result)
    {
        var parsed = IdentificationParseHelper.TryParse2(value, provider, [nameof(ForumThreadIdentification), "ForumComment"]);

        if (parsed != null)
        {
            result = new ForumThreadIdentification(parsed.Value.i1, parsed.Value.i2);
            return true;
        }

        result = null;
        return false;
    }

    public int CompareTo(ForumThreadIdentification? other) => Comparer<int>.Default.Compare(ThreadId, other?.ThreadId ?? -1);

}
