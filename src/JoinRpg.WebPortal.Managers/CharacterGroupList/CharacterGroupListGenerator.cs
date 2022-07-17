using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

internal class CharacterGroupListGenerator
{
    private CharacterGroup Root { get; }

    private IList<int> AlreadyOutputedGroups { get; } = new List<int>();

    private List<CharacterGroupDto> Results { get; } = new List<CharacterGroupDto>();

    public CharacterGroupListGenerator(CharacterGroup root, int? currentUserId)
    {
        Root = root;
        CurrentUserId = currentUserId;
    }

    public List<CharacterGroupDto> Generate()
    {
        GenerateFrom(Root, 0, new List<CharacterGroup>());
        return Results;
    }

    private void GenerateFrom(CharacterGroup characterGroup, int deepLevel, IList<CharacterGroup> pathToTop)
    {
        if (AlreadyOutputedGroups.Contains(characterGroup.CharacterGroupId))
        {
            return;
        }
        var vm = new CharacterGroupDto(characterGroup.CharacterGroupId, characterGroup.CharacterGroupName, pathToTop.Skip(1).Select(cg => cg.CharacterGroupName).ToArray(), characterGroup.IsPublic);

        Results.Add(vm);

        AlreadyOutputedGroups.Add(characterGroup.CharacterGroupId);

        foreach (var childGroup in characterGroup.GetOrderedChildGroups().Where(g => !g.IsSpecial).Where(c => c.IsActive && c.IsVisible(CurrentUserId)))
        {
            GenerateFrom(childGroup, deepLevel + 1, pathToTop.Append(characterGroup).ToList());
        }
    }

    private int? CurrentUserId { get; }
}
