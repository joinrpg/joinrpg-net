using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

internal class CharacterGroupListGenerator(CharacterGroup root, int? currentUserId)
{
    private IList<int> AlreadyOutputedGroups { get; } = [];

    private List<CharacterGroupDto> Results { get; } = [];

    public List<CharacterGroupDto> Generate()
    {
        GenerateFrom(root, []);
        return Results;
    }

    private void GenerateFrom(CharacterGroup characterGroup, IList<CharacterGroup> pathToTop)
    {
        if (AlreadyOutputedGroups.Contains(characterGroup.CharacterGroupId))
        {
            return;
        }
        var vm = new CharacterGroupDto(characterGroup.GetId(), characterGroup.CharacterGroupName, pathToTop.Skip(1).Select(cg => cg.CharacterGroupName).ToArray(), characterGroup.IsPublic, characterGroup.IsSpecial);

        Results.Add(vm);

        AlreadyOutputedGroups.Add(characterGroup.CharacterGroupId);

        foreach (var childGroup in characterGroup.GetOrderedChildGroups().Where(c => c.IsActive && c.IsVisible(currentUserId)))
        {
            GenerateFrom(childGroup, pathToTop.Append(characterGroup).ToList());
        }
    }
}
