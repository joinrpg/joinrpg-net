namespace JoinRpg.PrimitiveTypes
{
    public record UserFullName(PrefferedName PrefferedName, BornName? GivenName, SurName? SurName, FatherName? FatherName)
    {
    }

    public record BornName(string Value) : SingleValueType(Value)
    {
        public static BornName? FromOptional(string? value) => value == null ? null : new BornName(value);
    }
    public record SurName(string Value) : SingleValueType(Value)
    {
        public static SurName? FromOptional(string? value) => value == null ? null : new SurName(value);
    }
    public record FatherName(string Value) : SingleValueType(Value)
    {
        public static FatherName? FromOptional(string? value) => value == null ? null : new FatherName(value);
    }
    public record PrefferedName(string Value) : SingleValueType(Value)
    {
        public static PrefferedName? FromOptional(string? value) => value == null ? null : new PrefferedName(value);
    }
}
