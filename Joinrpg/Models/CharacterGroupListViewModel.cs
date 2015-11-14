using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
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

    public static CharacterGroupListViewModel FromGroup(CharacterGroup @group, bool hasMasterAccess)
    {
      return new CharacterGroupListViewModel
      {
        Groups = new CharacterGroupHierarchyBuilder(@group, hasMasterAccess).Generate(),
      }; 
    }

    public static CharacterGroupListViewModel FromGroupAsMaster(CharacterGroup group) => FromGroup(@group, true);

    //TODO: unit tests
    private class CharacterGroupHierarchyBuilder
    {
      private CharacterGroup Root { get; }

      private IList<int> AlreadyOutputedGroups { get; } = new List<int>();
      private IList<int> AlreadyOutputedChars { get; } = new List<int>();

      private IList<CharacterGroupListItemViewModel> Results { get; } = new List<CharacterGroupListItemViewModel>();

      private bool HasMasterAccess { get; }

      public CharacterGroupHierarchyBuilder(CharacterGroup root, bool hasMasterAccess)
      {
        Root = root;
        HasMasterAccess = hasMasterAccess;
      }

      public IList<CharacterGroupListItemViewModel> Generate()
      {
        GenerateFrom(Root, 0, new List<CharacterGroup>());
        return Results;
      }

      private void GenerateFrom(CharacterGroup characterGroup, int deepLevel, IList<CharacterGroup> pathToTop)
      {
        var immediateParent = pathToTop.LastOrDefault();

        var siblings = immediateParent?.GetOrderedChildGroups() ?? new List<CharacterGroup> { characterGroup};

        var vm = new CharacterGroupListItemViewModel
        {
          CharacterGroupId = characterGroup.CharacterGroupId,
          DeepLevel = deepLevel,
          Name = characterGroup.CharacterGroupName,
          FirstCopy = !AlreadyOutputedGroups.Contains(characterGroup.CharacterGroupId),
          AvaiableDirectSlots = characterGroup.HaveDirectSlots ?  characterGroup.AvaiableDirectSlots : 0,
          Characters = characterGroup.GetOrderedCharacters().Select(character => GenerateCharacter(character, characterGroup)).ToList(),
          Description = characterGroup.Description.ToHtmlString(),
          ActiveClaimsCount = characterGroup.Claims.Count(c => c.IsActive),
          Path = pathToTop.Select(cg => Results.First(item => item.CharacterGroupId == cg.CharacterGroupId)),
          IsPublic = characterGroup.IsPublic,
          IsActive = characterGroup.IsActive,
          FirstInGroup = siblings.First() == characterGroup,
          LasInGroup = siblings.Last() == characterGroup,
          ProjectId = characterGroup.ProjectId,
          RootGroupId = Root.CharacterGroupId
        };
        Results.Add(vm);

        if (!vm.FirstCopy)
          return;

        AlreadyOutputedGroups.Add(characterGroup.CharacterGroupId);

        foreach (var childGroup in characterGroup.GetOrderedChildGroups())
        {
          var characterGroups =  pathToTop.Union(new [] { characterGroup }).ToList();
          GenerateFrom(childGroup, deepLevel + 1, characterGroups);
        }
      }

      private CharacterViewModel GenerateCharacter(Character arg, CharacterGroup group)
      {
        var siblings = group.GetOrderedCharacters() ?? new List<Character> { arg };

        var vm = new CharacterViewModel
        {
          CharacterId = arg.CharacterId,
          CharacterName = arg.CharacterName,
          IsFirstCopy = !AlreadyOutputedChars.Contains(arg.CharacterId),
          IsAvailable = arg.IsAvailable,
          Description =  arg.Description.ToHtmlString(),
          IsPublic =  arg.IsPublic,
          IsActive = arg.IsActive,
          HidePlayer = arg.HidePlayerForCharacter,
          ActiveClaimsCount = arg.Claims.Count(claim => claim.IsActive),
          Player = arg.ApprovedClaim?.Player,
          HasMasterAccess = HasMasterAccess,
          ProjectId = arg.ProjectId,
          FirstInGroup = siblings.First() == arg,
          LastInGroup = siblings.Last() == arg,
          ParentCharacterGroupId = group.CharacterGroupId,
          RootGroupId = Root.CharacterGroupId
        };
        if (vm.IsFirstCopy)
        {
          AlreadyOutputedChars.Add(vm.CharacterId);
        }
        return vm;
      }
    }

    public static CharacterGroupListViewModel FromProjectAsMaster(Project project) => FromGroupAsMaster(project.RootGroup);
  }
}