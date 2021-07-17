namespace JoinRpg.Web.CharacterGroups
{
    public record CharacterGroupDto(int CharacterGroupId, string Name, string[] FullPath, bool IsPublic)
    {
    }
}
