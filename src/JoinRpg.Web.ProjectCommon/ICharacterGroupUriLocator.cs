namespace JoinRpg.Web.ProjectCommon;

public interface ICharacterGroupUriLocator
{
    Uri GetClaimListUri(CharacterGroupIdentification groupId);
    Uri GetCharacterListUri(CharacterGroupIdentification groupId);
    Uri GetReportUri(CharacterGroupIdentification groupId);
    Uri GetSubscribeUri(CharacterGroupIdentification groupId);
    Uri GetEditUri(CharacterGroupIdentification groupId);
    Uri GetDeleteUri(CharacterGroupIdentification groupId);
    Uri GetCreateCharacterUri(CharacterGroupIdentification groupId);
    Uri GetAddGroupUri(CharacterGroupIdentification groupId);
}
