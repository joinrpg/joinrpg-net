namespace JoinRpg.Web.ProjectCommon;

public interface ICharacterUriLocator
{
    Uri GetDetailsUri(CharacterIdentification characterId);
    Uri GetAddClaimUri(CharacterIdentification characterId);
}
