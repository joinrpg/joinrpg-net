using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Models
{
  
  public class CharacterGroupListViewModel
  {
    private IList<CharacterGroupListItemViewModel> Groups { get; set; }

    public IEnumerable<CharacterGroupListItemViewModel> PossibleParentsForGroup(int characterGroupId)
    {
      return
        ActiveGroups.Where(
          listItem =>
            listItem.CharacterGroupId != characterGroupId &&
            listItem.Path.All(cg => cg.CharacterGroupId != characterGroupId));
    }

    public IEnumerable<CharacterGroupListItemViewModel> PublicGroups
    {
      get { return Groups.Where(listItem => listItem.IsPublic && listItem.IsActive); }
    }

    public IEnumerable<CharacterGroupListItemViewModel> ActiveGroups
    {
      get { return Groups.Where(listItem => listItem.IsActive); }
    }

    public static CharacterGroupListViewModel FromGroup(CharacterGroup @group)
    {
      return new CharacterGroupListViewModel
      {
        Groups = new CharacterGroupHierarchyBuilder(group).Generate(),
      };
    }

    public static CharacterGroupListViewModel FromGroupAsMaster(CharacterGroup group) => FromGroup(group);

    //TODO: unit tests
    private class CharacterGroupHierarchyBuilder
    {
      private CharacterGroup Root { get; }

      private IList<int> AlreadyOutputedGroups { get; } = new List<int>();
      private IList<int> AlreadyOutputedChars { get; } = new List<int>();

      private IList<CharacterGroupListItemViewModel> Results { get; } = new List<CharacterGroupListItemViewModel>();

      public CharacterGroupHierarchyBuilder(CharacterGroup root)
      {
        Root = root;
      }

      public IList<CharacterGroupListItemViewModel> Generate()
      {
        GenerateFrom(Root, 0, new List<CharacterGroup>());
        return Results;
      }

      private void GenerateFrom(CharacterGroup characterGroup, int deepLevel, IList<CharacterGroup> pathToTop)
      {
        var vm = new CharacterGroupListItemViewModel()
        {
          CharacterGroupId = characterGroup.CharacterGroupId,
          DeepLevel = deepLevel,
          Name = characterGroup.CharacterGroupName,
          FirstCopy = !AlreadyOutputedGroups.Contains(characterGroup.CharacterGroupId),
          AvaiableDirectSlots = characterGroup.HaveDirectSlots ?  characterGroup.AvaiableDirectSlots : 0,
          Characters = characterGroup.Characters.Select(GenerateCharacter).ToList(),
          Description = characterGroup.Description.ToHtmlString(),
          Path = pathToTop.Select(cg => Results.First(item => item.CharacterGroupId == cg.CharacterGroupId)),
          IsPublic = characterGroup.IsPublic,
          IsActive = characterGroup.IsActive
        };
        Results.Add(vm);

        if (!vm.FirstCopy)
          return;

        AlreadyOutputedGroups.Add(characterGroup.CharacterGroupId);

        foreach (var childGroup in characterGroup.ChildGroups)
        {
          var characterGroups =  pathToTop.Union(new [] { characterGroup }).ToList();
          GenerateFrom(childGroup, deepLevel + 1, characterGroups);
        }
      }

      private CharacterViewModel GenerateCharacter(Character arg)
      {
        var vm = new CharacterViewModel()
        {
          CharacterId = arg.CharacterId,
          CharacterName = arg.CharacterName,
          IsFirstCopy = !AlreadyOutputedChars.Contains(arg.CharacterId),
          IsAvailable = arg.IsAvailable,
          Description =  arg.Description.ToHtmlString(),
          IsPublic =  arg.IsPublic
        };
        if (vm.IsFirstCopy)
        {
          AlreadyOutputedChars.Add(vm.CharacterId);
        }
        return vm;
      }
    }
  }
}