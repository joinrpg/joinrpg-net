using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

internal class CharacterGroupListGenerator(ProjectInfo projectInfo, UserIdentification? currentUserId)
{
    private HashSet<CharacterGroupIdentification> AlreadyOutputedGroups { get; } = [];

    private List<CharacterGroupDto> Results { get; } = [];

    public List<CharacterGroupDto> Generate()
    {
        GenerateFrom(projectInfo.RootCharacterGroupId, []);
        return Results;
    }

    private void GenerateFrom(CharacterGroupIdentification groupId, IList<string> pathToTop)
    {
        if (!AlreadyOutputedGroups.Add(groupId))
        {
            return;
        }

        var group = projectInfo.Groups[groupId];
        var vm = new CharacterGroupDto(groupId, group.Name, pathToTop.Skip(1).ToArray(), group.IsPublic, group.IsSpecial);

        Results.Add(vm);

        foreach (var childId in group.DirectChildGroupIds)
        {
            var child = projectInfo.Groups[childId];
            if (child.IsActive && (child.IsPublic || projectInfo.PublishPlot || projectInfo.HasMasterAccess(currentUserId)))
            {
                GenerateFrom(childId, pathToTop.Append(group.Name).ToList());
            }
        }
    }
}
