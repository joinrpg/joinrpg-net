using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.DataModel;

/// <summary>
/// Хранимое в БД представление markdown-разметки (EF6 ComplexType, раскладывается
/// в колонку <c>{PropertyName}_Contents</c>). Для бизнес-логики используется
/// <see cref="MarkdownString"/>; между типами есть неявные конверсии.
///
/// Не надо использовать за пределами DataModel, предпочитайте MarkdownString
/// </summary>
[ComplexType]
public class MarkdownDbValue : IEquatable<MarkdownDbValue>
{
    public MarkdownDbValue(string? contents) => Contents = contents;

    public MarkdownDbValue() : this(null)
    {
    }

    public string? Contents { get; private set; }

    public static implicit operator MarkdownString?(MarkdownDbValue? value)
        => value?.Contents is null ? null : new MarkdownString(value.Contents);

    public static implicit operator MarkdownDbValue?(MarkdownString? value)
        => value is null ? null : new MarkdownDbValue(value.Value);

    public override string ToString() => $"Markdown({Contents})";

    public bool Equals(MarkdownDbValue? other) => other is MarkdownDbValue && string.Equals(Contents, other.Contents);

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return Equals(obj as MarkdownDbValue);
    }

    public override int GetHashCode()
    {
        // It's not good to use mutable members in GetHashCode.
        // However I didn't manage to make it readonly becasue EF wanted a setter.
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Contents?.GetHashCode() ?? 0;
    }

    public static bool operator ==(MarkdownDbValue string1, MarkdownDbValue string2)
    {
        return string1?.Equals(string2) ?? string2 is null;
    }

    public static bool operator !=(MarkdownDbValue string1, MarkdownDbValue string2)
    {
        return !(string1 == string2);
    }
}
