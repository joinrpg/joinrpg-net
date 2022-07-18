using JoinRpg.Helpers;
namespace JoinRpg.PrimitiveTypes;

public record Email : SingleValueType<string>
{
    public Email(string value)
        : base(CheckEmail(value))
    {

    }
    public static string CheckEmail(string value)
    {
        if (!value.Contains('@'))
        {
            throw new ArgumentException("Incorrect email", nameof(value));
        }
        return value;
    }

    public string UserPart => Value.TakeWhile(ch => ch != '@').AsString();
}
