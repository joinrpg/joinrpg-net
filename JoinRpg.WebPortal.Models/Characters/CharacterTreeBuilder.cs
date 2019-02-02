using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Characters
{
  internal class CharacterTreeBuilder
  {
    private CharacterGroup Root { get; }

    private IList<int> AlreadyOutputedGroups { get; } = new List<int>();
    private IList<int> AlreadyOutputedChars { get; } = new List<int>();

    private IList<CharacterTreeItem> Results { get; } = new List<CharacterTreeItem>();

    public CharacterTreeBuilder(CharacterGroup root, int? currentUserId)
    {
      Root = root;
      CurrentUserId = currentUserId;
    }

    [MustUseReturnValue]
    public IList<CharacterTreeItem> Generate()
    {
      GenerateFrom(Root, 0, new List<CharacterGroup>());
      return Results;
    }

    private CharacterTreeItem GenerateFrom(CharacterGroup characterGroup, int deepLevel, IList<CharacterGroup> pathToTop)
    {
      var vm = new CharacterTreeItem (characterGroup)
      {
        DeepLevel = deepLevel,
        FirstCopy = !AlreadyOutputedGroups.Contains(characterGroup.CharacterGroupId),
        Characters = characterGroup.GetOrderedCharacters().Where(c => c.IsActive && c.IsVisible(CurrentUserId)).Select(GenerateCharacter).ToList(),
        Path = pathToTop.Select(cg => Results.First(item => item.CharacterGroupId == cg.CharacterGroupId)),
        IsSpecial = characterGroup.IsSpecial,
      };

      Results.Add(vm);

      if (!vm.FirstCopy)
      {
        var prevCopy = Results.Single(cg => cg.FirstCopy && cg.CharacterGroupId == vm.CharacterGroupId);
        vm.ChildGroups = prevCopy.ChildGroups;
        return vm;
      }

      AlreadyOutputedGroups.Add(characterGroup.CharacterGroupId);

      var childs = new List<CharacterTreeItem>();

      foreach (var childGroup in characterGroup.GetOrderedChildGroups().OrderBy(g => g.IsSpecial).Where(c => c.IsActive && c.IsVisible(CurrentUserId)))
      {
        var characterGroups = pathToTop.Union(new[] { characterGroup }).ToList();
        var child = GenerateFrom(childGroup, deepLevel + 1, characterGroups);
        childs.Add(child);
      }

      vm.ChildGroups = childs;        

      return vm;
    }

    private int? CurrentUserId { get; }

    private CharacterLinkViewModel GenerateCharacter(Character arg)
    {
      var vm = new CharacterLinkViewModel(arg)
      {
        IsFirstCopy = !AlreadyOutputedChars.Contains(arg.CharacterId),
      };
      if (vm.IsFirstCopy)
      {
        AlreadyOutputedChars.Add(vm.CharacterId);
      }
      return vm;
    }
  }
}