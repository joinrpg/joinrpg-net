namespace JoinRpg.Web.ProjectCommon;

public record CharacterGroupDto(CharacterGroupIdentification CharacterGroupId, string Name, string[] FullPath, bool IsPublic)
{
}
