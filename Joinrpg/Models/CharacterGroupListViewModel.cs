using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
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

      private CharacterGroupListItemViewModel GenerateFrom(CharacterGroup characterGroup, int deepLevel, IList<CharacterGroup> pathToTop)
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
          IsAcceptingClaims = characterGroup.HaveDirectSlots && characterGroup.Project.IsAcceptingClaims && characterGroup.AvaiableDirectSlots != 0,
          Characters = characterGroup.GetOrderedCharacters().Select(character => GenerateCharacter(character, characterGroup)).ToList(),
          Description = characterGroup.Description.ToHtmlString(),
          ActiveClaimsCount = characterGroup.Claims.Count(c => c.IsActive),
          Path = pathToTop.Select(cg => Results.First(item => item.CharacterGroupId == cg.CharacterGroupId)),
          IsPublic = characterGroup.IsPublic,
          IsSpecial = characterGroup.IsSpecial,
          IsActive = characterGroup.IsActive,
          IsRootGroup = characterGroup.IsRoot,
          FirstInGroup = siblings.First() == characterGroup,
          LastInGroup = siblings.Last() == characterGroup,
          ProjectId = characterGroup.ProjectId,
          RootGroupId = Root.CharacterGroupId,
        };

        if (vm.IsSpecial)
        {
          var variant =
            characterGroup.Project.ProjectFields.SelectMany(pf => pf.DropdownValues)
              .SingleOrDefault(pfv => pfv.CharacterGroup == characterGroup);

          if (variant != null)
          {
            vm.BoundExpression = $"{variant.ProjectField.FieldName} = {variant.Label}";
          }
        }
        
        Results.Add(vm);

        if (!vm.FirstCopy)
        {
          var prevCopy = Results.Single(cg => cg.FirstCopy && cg.CharacterGroupId == vm.CharacterGroupId);
          vm.ChildGroups = prevCopy.ChildGroups;
          vm.TotalSlots = prevCopy.TotalSlots;
          vm.TotalCharacters = prevCopy.TotalCharacters;
          vm.TotalCharactersWithPlayers = prevCopy.TotalCharactersWithPlayers;
          vm.TotalDiscussedClaims = prevCopy.TotalDiscussedClaims;
          vm.TotalActiveClaims = prevCopy.TotalActiveClaims;
          vm.TotalNpcCharacters = prevCopy.TotalNpcCharacters;
          return vm;
        }

        AlreadyOutputedGroups.Add(characterGroup.CharacterGroupId);

        var childs = new List<CharacterGroupListItemViewModel>();

        foreach (var childGroup in characterGroup.GetOrderedChildGroups())
        {
          var characterGroups =  pathToTop.Union(new [] { characterGroup }).ToList();
          var child = GenerateFrom(childGroup, deepLevel + 1, characterGroups);
          childs.Add(child);
        }

        vm.ChildGroups = childs;

        var totalChildsBeforeFlat = vm.FlatTree(model => model.ChildGroups).ToList();
        var flatChilds = totalChildsBeforeFlat.Distinct().ToList();
        var flatCharacters = flatChilds.SelectMany(c => c.Characters.Where(ch => ch.IsActive)).Distinct().ToList();

        vm.TotalSlots = flatChilds.Sum(c => c.AvaiableDirectSlots == -1 ? 0 : c.AvaiableDirectSlots) +
                        flatCharacters.Count(c => c.IsAvailable);
        
        vm.TotalCharacters = flatCharacters.Count + flatChilds.Sum(c => c.AvaiableDirectSlots == -1 ? 0 : c.AvaiableDirectSlots);
        vm.TotalNpcCharacters = flatCharacters.Count(c => !c.IsAcceptingClaims);
        vm.TotalCharactersWithPlayers = flatCharacters.Count(c => c.Player != null);

        vm.TotalDiscussedClaims = flatCharacters.Where(c => c.Player == null).Sum(c => c.ActiveClaimsCount) + flatChilds.Sum(c => c.ActiveClaimsCount);
        vm.TotalActiveClaims = flatCharacters.Sum(c => c.ActiveClaimsCount) + flatChilds.Sum(c => c.ActiveClaimsCount);

        return vm;
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
          RootGroupId = Root.CharacterGroupId,
          IsHot = arg.IsHot && arg.IsAvailable,
          IsAcceptingClaims =  arg.IsAcceptingClaims
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