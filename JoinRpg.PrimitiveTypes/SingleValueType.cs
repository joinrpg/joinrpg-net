namespace JoinRpg.PrimitiveTypes
{
    public record SingleValueType(string Value)
    {
        public static implicit operator string(SingleValueType type) => type.Value;
    }
}
