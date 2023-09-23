using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Characters;

public class CharacterTreeBuilder
{
    private CharacterGroup Root { get; }

    private readonly Dictionary<int, CharacterLinkViewModel> alreadyOutputedChars = new();

    private IList<CharacterTreeItem> Results { get; } = new List<CharacterTreeItem>();

    public CharacterTreeBuilder(CharacterGroup root, int? currentUserId)
    {
        Root = root;
        CurrentUserId = currentUserId;
    }

    public IList<CharacterTreeItem> Generate()
    {
        _ = GenerateFrom(Root, 0, new List<CharacterGroup>());
        return Results;
    }

    private CharacterTreeItem GenerateFrom(CharacterGroup characterGroup, int deepLevel, IList<CharacterGroup> pathToTop)
    {
        var prevCopy = Results.SingleOrDefault(cg => cg.FirstCopy && cg.CharacterGroupId == characterGroup.CharacterGroupId);

        var vm = new CharacterTreeItem(characterGroup)
        {
            DeepLevel = deepLevel,
            FirstCopy = prevCopy is null,
            Characters = characterGroup.GetOrderedCharacters().Where(c => c.IsActive && c.IsVisible(CurrentUserId)).Select(GenerateCharacter).ToList(),
            Path = pathToTop.Select(cg => Results.First(item => item.CharacterGroupId == cg.CharacterGroupId)),
            IsSpecial = characterGroup.IsSpecial,
            ChildGroups = prevCopy?.ChildGroups!, //Will be set later
        };

        Results.Add(vm); // Надо добавить в список перед тем, как добавлять детей, иначе клиент сходит с ума

        if (vm.ChildGroups is null)
        {
            vm.ChildGroups = CreateChilds().ToList();
        }

        return vm;

        IEnumerable<CharacterTreeItem> CreateChilds()
        {
            foreach (var childGroup in characterGroup.GetOrderedChildGroups().OrderBy(g => g.IsSpecial).Where(c => c.IsActive && c.IsVisible(CurrentUserId)))
            {
                var characterGroups = pathToTop.Union(new[] { characterGroup }).ToList();
                yield return GenerateFrom(childGroup, deepLevel + 1, characterGroups);
            }
        }
    }

    private int? CurrentUserId { get; }

    private CharacterLinkViewModel GenerateCharacter(Character arg)
    {
        if (alreadyOutputedChars.TryGetValue(arg.CharacterId, out var prevCopy))
        {
            return prevCopy;
        }
        var vm = new CharacterLinkViewModel(arg);
        alreadyOutputedChars.Add(arg.CharacterId, vm);

        return vm;
    }
}
