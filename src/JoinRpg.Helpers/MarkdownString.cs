using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel;

[ComplexType]
public class MarkdownString : IEquatable<MarkdownString>
{
    public MarkdownString(string? contents) =>
        //TODO: Validate for correct Markdown
        Contents = contents;

    public MarkdownString() : this(null)
    {
    }

    public string? Contents { get; private set; }

    public override string ToString() => $"Markdown({Contents})";

    public bool Equals(MarkdownString? other) => other is MarkdownString && string.Equals(Contents, other.Contents);

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return Equals(obj as MarkdownString);
    }

    public override int GetHashCode()
    {
        // It's not good to use mutable members in GetHashCode.
        // However I didn't manage to make it readonly becasue EF wanted a setter.
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Contents?.GetHashCode() ?? 0;
    }

    public static bool operator ==(MarkdownString string1, MarkdownString string2)
    {
        return string1?.Equals(string2) ?? string2 is null;
    }

    public static bool operator !=(MarkdownString string1, MarkdownString string2)
    {
        return !(string1 == string2);
    }
}
