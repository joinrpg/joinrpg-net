namespace JoinRpg.Services.Email;
public static class NotificationRegistration
{
    public static IEnumerable<Type> GetTypes()
    {
        yield return typeof(EmailServiceImpl);
        yield return typeof(MasterEmailServiceImpl);
    }
}
