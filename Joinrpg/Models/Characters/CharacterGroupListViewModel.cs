using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Characters
{
  
  public static class CharacterGroupListViewModel
  {
    [MustUseReturnValue]
    public static IEnumerable<CharacterGroupListItemViewModel> GetGroups(CharacterGroup field, int? currentUserId)
    {
      return new CharacterGroupHierarchyBuilder(field, currentUserId).Generate().WhereNotNull();
    }

    //TODO: unit tests
    private class CharacterGroupHierarchyBuilder
    {
      private CharacterGroup Root { get; }

      private IList<int> AlreadyOutputedChars { get; } = new List<int>();

      private IList<CharacterGroupListItemViewModel> Results { get; } = new List<CharacterGroupListItemViewModel>();

      private bool HasMasterAccess { get; }

      private int? CurrentUserId { get; }

      public CharacterGroupHierarchyBuilder(CharacterGroup root, int? currentUserId)
      {
        Root = root;
        HasMasterAccess = root.HasMasterAccess(currentUserId);
        HasEditRolesAccess = root.HasEditRolesAccess(currentUserId);
        CurrentUserId = currentUserId;
      }

      public IList<CharacterGroupListItemViewModel> Generate()
      {
        GenerateFrom(Root, 0, new List<CharacterGroup>());
        return Results;
      } 

      private CharacterGroupListItemViewModel GenerateFrom(CharacterGroup characterGroup, int deepLevel, IList<CharacterGroup> pathToTop)
      {
        if (!characterGroup.IsVisible(CurrentUserId))
        {
          return null;
        }
        var prevCopy = Results.FirstOrDefault(cg => cg.FirstCopy && cg.CharacterGroupId == characterGroup.CharacterGroupId);

        var vm = new CharacterGroupListItemViewModel
        {
          CharacterGroupId = characterGroup.CharacterGroupId,
          DeepLevel = deepLevel,
          Name = characterGroup.CharacterGroupName,
          FirstCopy = prevCopy == null,
          AvaiableDirectSlots = characterGroup.HaveDirectSlots ? characterGroup.AvaiableDirectSlots : 0,
          IsAcceptingClaims = characterGroup.IsAcceptingClaims(),
          ActiveCharacters =
            prevCopy?.ActiveCharacters ??
            GenerateCharacters(characterGroup)
              .ToList(),
          Description = characterGroup.Description.ToHtmlString(),
          ActiveClaimsCount = characterGroup.Claims.Count(c => c.IsActive),
          Path = pathToTop.Select(cg => Results.First(item => item.CharacterGroupId == cg.CharacterGroupId)),
          IsPublic = characterGroup.IsPublic,
          IsSpecial = characterGroup.IsSpecial,
          IsActive = characterGroup.IsActive,
          IsRootGroup = characterGroup.IsRoot,
          ProjectId = characterGroup.ProjectId,
          RootGroupId = Root.CharacterGroupId,
        };

        if (Root == characterGroup)
        {
          vm.First = true;
          vm.Last = true;
        }

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

        if (prevCopy != null)
        {
          vm.ChildGroups = prevCopy.ChildGroups;
          vm.TotalSlots = prevCopy.TotalSlots;
          vm.TotalCharacters = prevCopy.TotalCharacters;
          vm.TotalCharactersWithPlayers = prevCopy.TotalCharactersWithPlayers;
          vm.TotalDiscussedClaims = prevCopy.TotalDiscussedClaims;
          vm.TotalActiveClaims = prevCopy.TotalActiveClaims;
          vm.TotalNpcCharacters = prevCopy.TotalNpcCharacters;
          vm.Unlimited = prevCopy.Unlimited;
          vm.TotalAcceptedClaims = prevCopy.TotalAcceptedClaims;
          return vm;
        }
        
        var childGroups = characterGroup.GetOrderedChildGroups().OrderBy(g => g.IsSpecial).Where(g =>g.IsActive && g.IsVisible(CurrentUserId)).ToList();
        var pathForChildren = pathToTop.Union(new[] { characterGroup }).ToList();

        vm.ChildGroups = childGroups.Select(childGroup => GenerateFrom(childGroup, deepLevel + 1, pathForChildren)).ToList().MarkFirstAndLast();

        var flatChilds = vm.FlatTree(model => model.ChildGroups).Distinct().ToList();

        var flatCharacters = flatChilds.SelectMany(c => c.ActiveCharacters).Distinct().ToList();

        vm.TotalSlots = flatChilds.Sum(c => c.AvaiableDirectSlots == -1 ? 0 : c.AvaiableDirectSlots) +
                        flatCharacters.Count(c => c.IsAvailable);
        
        vm.TotalCharacters = flatCharacters.Count + flatChilds.Sum(c => c.AvaiableDirectSlots == -1 ? 0 : c.AvaiableDirectSlots);
        vm.TotalNpcCharacters = flatCharacters.Count(c => !c.IsAcceptingClaims);
        vm.TotalCharactersWithPlayers = flatCharacters.Count(c => c.Player != null);

        vm.TotalDiscussedClaims = flatCharacters.Where(c => c.Player == null).Sum(c => c.ActiveClaimsCount) + flatChilds.Sum(c => c.ActiveClaimsCount);
        vm.TotalActiveClaims = flatCharacters.Sum(c => c.ActiveClaimsCount) + flatChilds.Sum(c => c.ActiveClaimsCount);
        vm.TotalAcceptedClaims = flatCharacters.Count(c => c.Player != null);
        vm.Unlimited = vm.AvaiableDirectSlots == -1 || flatChilds.Any(c => c.AvaiableDirectSlots == -1);

        return vm;
      }

      private IEnumerable<CharacterViewModel> GenerateCharacters(CharacterGroup characterGroup)
      {
        var characters = characterGroup.GetOrderedCharacters().Where(c => c.IsActive && c.IsVisible(CurrentUserId)).ToArray();

        return characters.Select(character => GenerateCharacter(character, characterGroup,characters));
      }

      private CharacterViewModel GenerateCharacter(Character arg, CharacterGroup group, IReadOnlyList<Character> siblings)
      {
        var vm = new CharacterViewModel
        {
          CharacterId = arg.CharacterId,
          CharacterName = arg.CharacterName,
          IsFirstCopy = !AlreadyOutputedChars.Contains(arg.CharacterId),
          IsAvailable = arg.IsAvailable,
          Description =  arg.Description.ToHtmlString(),
          IsPublic =  arg.IsPublic,
          IsActive = arg.IsActive,
          HidePlayer = arg.HidePlayerForCharacter && !arg.Project.IsPlotPublished(),
          ActiveClaimsCount = arg.Claims.Count(claim => claim.IsActive),
          Player = arg.ApprovedClaim?.Player,
          HasMasterAccess = HasMasterAccess,
          HasEditRolesAccess = HasEditRolesAccess,
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

      private bool HasEditRolesAccess { get; }
    }
  }
}