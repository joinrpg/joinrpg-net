namespace JoinRpg.Domain;

public static class UserExtensions
{
    public static UserIdentification GetId(this User user) => new UserIdentification(user.UserId);

    /// <summary>
    /// Returns display name of a user
    /// </summary>
    public static string GetDisplayName(this User user)
    {
        return user.ExtractDisplayName().DisplayName;
    }
}
