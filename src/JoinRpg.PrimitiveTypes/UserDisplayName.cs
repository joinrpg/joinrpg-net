namespace JoinRpg.PrimitiveTypes;

[method: System.Text.Json.Serialization.JsonConstructor]
public record UserDisplayName(
    string DisplayName,
    string? FullName)
{
    public UserDisplayName(UserFullName fullName, Email email) : this(GetDisplayName(fullName, email), fullName?.FullName)
    {
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
