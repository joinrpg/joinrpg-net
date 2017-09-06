using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
  [ComplexType]
  public class MarkdownString
  {
    public MarkdownString([CanBeNull] string contents)
    {
      //TODO: Validate for correct Markdown
      Contents = contents;
    }

    public MarkdownString() : this(null)
    {
    }

    [CanBeNull]
    public string Contents { get; private set; }

    public override string ToString() => $"Markdown({Contents})";

    protected bool Equals(MarkdownString other)
    {
      return string.Equals(Contents, other.Contents);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
        return false;
      if (ReferenceEquals(this, obj))
        return true;
      return obj.GetType() == this.GetType() && Equals((MarkdownString)obj);
    }

    public override int GetHashCode()
    {
      // It's not good to use mutable members in GetHashCode.
      // However I didn't manage to make it readonly becasue EF wanted a setter.
      return Contents?.GetHashCode() ?? 0;
    }

    public static bool operator ==(MarkdownString string1, MarkdownString string2)
    {
      return string1?.Equals(string2) ?? ReferenceEquals(string2, null);
    }

    public static bool operator !=(MarkdownString string1, MarkdownString string2)
    {
      return !(string1 == string2);
    }
  }
}
