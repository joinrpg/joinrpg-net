using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.PrimitiveTypes;

public record SingleValueType<T>(T Value) : IComparable<SingleValueType<T>>, IComparable<T> where T : IComparable<T>
{
    [return: NotNullIfNotNull(nameof(type))]
    public static implicit operator T?(SingleValueType<T>? type) => type is null ? default : type.Value;

    public override string ToString() => $"{GetType().Name}({Value})";
    int IComparable<SingleValueType<T>>.CompareTo(SingleValueType<T>? other) => other is null ? Value.CompareTo(default) : Value.CompareTo(other.Value);
    int IComparable<T>.CompareTo(T? other) => other is null ? Value.CompareTo(default) : Value.CompareTo(other);
}
