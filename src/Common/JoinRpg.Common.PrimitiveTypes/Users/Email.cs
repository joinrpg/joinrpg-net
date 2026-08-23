namespace JoinRpg.Common.PrimitiveTypes;

[TypedStringValue]
public partial record Email(string Value)
{
    internal static string? CustomValidateAndCanonicalize(string value)
    {
        if (!value.Contains('@'))
        {
            throw new ArgumentException("Incorrect email", nameof(value));
        }
        return value;
    }
    public string UserPart => string.Join("", Value.TakeWhile(ch => ch != '@'));
}
