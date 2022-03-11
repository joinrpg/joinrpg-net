namespace JoinRpg.PrimitiveTypes
{
    public record AvatarIdentification(int Value) : SingleValueType<int>(Value)
    {
        public static AvatarIdentification? FromOptional(int? value) => value == null ? null : new AvatarIdentification(value.Value);
    }
}
