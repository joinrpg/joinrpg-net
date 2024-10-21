namespace JoinRpg.Dal.Impl.Repositories;
//TODO: temporary duplication from domain
public static class UserTransformationExtensions
{
    public static UserFullName ExtractFullName(this User user)
    {
        return new UserFullName(
            PrefferedName.FromOptional(user.PrefferedName),
            BornName.FromOptional(user.BornName),
            SurName.FromOptional(user.SurName),
            FatherName.FromOptional(user.FatherName));
    }

    public static UserDisplayName ExtractDisplayName(this User user) => new UserDisplayName(user.ExtractFullName(), new Email(user.Email));
    public static UserDisplayName ExtractDisplayName(this User user)
    {
        return UserDisplayName.Create(user.ExtractFullName(), new Email(user.Email));
    }

    public static UserExternalLogin? TryGetExternalLoginByProviderId(this User user, string providerId)
    {
        return user.ExternalLogins.SingleOrDefault(l => l.Provider.Equals(providerId, StringComparison.InvariantCultureIgnoreCase));
    }

    public static TelegramId? TryGetTelegramId(this User user)
    {
        var elogin = user.TryGetExternalLoginByProviderId("telegram");
        if (elogin == null)
        {
            return null;
        }
        return new TelegramId(long.Parse(elogin.Key), PrefferedName.FromOptional(user.Extra?.Telegram));
    }
}
