namespace JoinRpg.WebPortal.Managers.Claims;

public interface IUserLinkResolver
{
    Task<UserIdentification> ResolveAsync(string userLink);
}
