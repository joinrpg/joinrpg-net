namespace JoinRpg.Domain;

public static class CharacterParentGroupExtensions
{
    public static IEnumerable<CharacterGroupIdentification> GetParentGroupIdsToTop(this Character target, ProjectInfo projectInfo)
        => projectInfo.GetParentGroupIdsIncludingThis(target.GetDirectGroupIds());

    public static IEnumerable<CharacterGroupIdentification> GetDirectGroupIds(this Character target)
        => CharacterGroupIdentification.FromList(target.ParentCharacterGroupIds, new(target.ProjectId));

    public static IEnumerable<CharacterGroupInfo> GetParentGroupsToTop(this Character target, ProjectInfo projectInfo)
        => projectInfo.GetParentGroupsIncludingThis(target.GetDirectGroupIds());

    public static IEnumerable<CharacterGroupInfo> GetIntrestingGroupsForDisplayToTop(this Character character, ProjectInfo projectInfo)
        => character.GetParentGroupsToTop(projectInfo).Where(g => g.IsIntresting);
}
