using System.Text.Json;

namespace JoinRpg.PrimitiveTypes;

// How to put attr on primary ctor??? [method:System.Text.Json.Serialization.JsonConstructor]
public record UserDisplayName(string DisplayName, string? FullName)
{
    // Cant make this constructor, or JSON deserializer fails to find primary ctor
    public static UserDisplayName Create(UserFullName fullName, Email email)
    {
        return new(GetDisplayName(fullName, email), fullName?.FullName);
    }

    private static string GetDisplayName(UserFullName fullName, Email email)
    {
        ArgumentNullException.ThrowIfNull(fullName);
        ArgumentNullException.ThrowIfNull(email);

        if (!string.IsNullOrWhiteSpace(fullName.PrefferedName?.Value))
        {
            return fullName.PrefferedName;
        }
        if (!string.IsNullOrWhiteSpace(fullName.FullName))
        {
            return fullName.FullName;
        }
        return email.UserPart;
    }

}
