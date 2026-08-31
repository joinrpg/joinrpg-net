namespace JoinRpg.Web.AdminTools;

public interface IUserAdminClient
{
    Task<UserAdminPanelViewModel> GetAdminPanel(UserIdentification userId);
    Task RemoveVkLink(UserIdentification userId);
    Task SetAdminFlag(UserIdentification userId, bool value);
    Task SetVerificationFlag(UserIdentification userId, bool value);
    Task ChangeEmail(UserIdentification userId, string newEmail);
}
