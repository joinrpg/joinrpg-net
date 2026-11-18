using System.Text.Json.Serialization;

namespace JoinRpg.Common.PrimitiveTypes;

[method: JsonConstructor]
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
                .Where(x => x != null));
        }
    }
}

[TypedStringValue]
public partial record BornName(string Value);
[TypedStringValue]
public partial record SurName(string Value);
[TypedStringValue]
public partial record FatherName(string Value);
[TypedStringValue]
public partial record PrefferedName(string Value);
