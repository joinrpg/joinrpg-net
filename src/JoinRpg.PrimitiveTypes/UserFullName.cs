using JoinRpg.Helpers;

namespace JoinRpg.PrimitiveTypes;

public record UserFullName(PrefferedName? PrefferedName, BornName? BornName, SurName? SurName, FatherName? FatherName)
{
    public string? FullName
    {
        get
        {
            if (BornName is null && SurName is null && FatherName is null)
            {
                return null;
            }
            return string.Join(" ",
                new string?[] { BornName?.Value, FatherName?.Value, SurName?.Value }
                .WhereNotNull());
        }
    }
}

public record BornName(string Value) : SingleValueType<string>(Value)
{
    public static BornName? FromOptional(string? value) => value == null ? null : new BornName(value);
}
public record SurName(string Value) : SingleValueType<string>(Value)
{
    public static SurName? FromOptional(string? value) => value == null ? null : new SurName(value);
}
public record FatherName(string Value) : SingleValueType<string>(Value)
{
    public static FatherName? FromOptional(string? value) => value == null ? null : new FatherName(value);
}
public record PrefferedName(string Value) : SingleValueType<string>(Value)
{
    public static PrefferedName? FromOptional(string? value) => value == null ? null : new PrefferedName(value);
}
