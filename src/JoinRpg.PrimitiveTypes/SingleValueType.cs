namespace JoinRpg.PrimitiveTypes;

public record SingleValueType<T>(T Value)
{
    public static implicit operator T(SingleValueType<T> type) => type.Value;

    public override string ToString() => $"{GetType().Name}({Value})";
}
