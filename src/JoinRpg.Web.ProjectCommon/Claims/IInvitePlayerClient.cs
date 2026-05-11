namespace JoinRpg.Web.ProjectCommon.Claims;

public interface IInvitePlayerClient
{
    Task<ClaimIdentification> InvitePlayer(CharacterIdentification characterId, string userLink);
}
