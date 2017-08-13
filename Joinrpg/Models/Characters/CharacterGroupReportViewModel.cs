using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models.Characters
{
  
  public static class CharacterGroupReportViewModel
  {
    [MustUseReturnValue]
    public static IEnumerable<CharacterGroupReportItemViewModel> GetGroups(CharacterGroup field)
    {
      return new CharacterGroupHierarchyBuilder(field).Generate().WhereNotNull();
    }

    //TODO: unit tests
    private class CharacterGroupHierarchyBuilder
    {
      private CharacterGroup Root { get; }

      private IList<CharacterGroupReportItemViewModel> Results { get; } = new List<CharacterGroupReportItemViewModel>();

      public CharacterGroupHierarchyBuilder(CharacterGroup root)
      {
        Root = root;
      }

      public IList<CharacterGroupReportItemViewModel> Generate()
      {
        GenerateFrom(Root, 0);
        foreach (var characterGroup in Root.ChildGroups)
        {
          GenerateFrom(characterGroup, 1);
        }
        return Results;
      } 

      private void GenerateFrom(CharacterGroup characterGroup, int deepLevel)
      {
        var vm = new CharacterGroupReportItemViewModel
        {
          CharacterGroupId = characterGroup.CharacterGroupId,
          DeepLevel = deepLevel,
          Name = characterGroup.CharacterGroupName,
          AvaiableDirectSlots = characterGroup.HaveDirectSlots ? characterGroup.AvaiableDirectSlots : 0,
          ActiveClaimsCount = characterGroup.Claims.Count(c => c.IsActive),
          IsPublic = characterGroup.IsPublic,
        };

        Results.Add(vm);

        var flatChilds = characterGroup.FlatTree(model => model.ChildGroups).Distinct().ToList();

        var flatCharacters = flatChilds.SelectMany(c => c.Characters).Where(c => c.IsActive).Distinct().ToList();

        vm.TotalSlots = flatChilds.Sum(c => c.AvaiableDirectSlots == -1 ? 0 : c.AvaiableDirectSlots) +
                        flatCharacters.Count(c => c.IsAvailable);
        
        vm.TotalCharacters = flatCharacters.Count + flatChilds.Sum(c => c.AvaiableDirectSlots == -1 ? 0 : c.AvaiableDirectSlots);
        vm.TotalNpcCharacters = flatCharacters.Count(c => !c.IsAcceptingClaims && c.ApprovedClaim == null);
        vm.TotalCharactersWithPlayers = flatCharacters.Count(c => c.ApprovedClaim  != null);
        vm.TotalInGameCharacters = flatCharacters.Count(c => c.InGame);

        vm.TotalDiscussedClaims =
          flatCharacters.Where(c => c.ApprovedClaim == null)
            .Sum(c => c.Claims.Count(claim => claim.IsActive)) +
          
            flatChilds.Sum(c => c.Claims.Count());
        vm.TotalActiveClaims = flatCharacters.Sum(c => c.Claims.Count(claim => claim.IsActive)) + flatChilds.Sum(c => c.Claims.Count());
        vm.TotalAcceptedClaims = flatCharacters.Count(c => c.ApprovedClaim != null);
        vm.TotalCheckedInClaims = flatCharacters.Count(c => c.ApprovedClaim?.CheckInDate != null);
        vm.Unlimited = vm.AvaiableDirectSlots == -1 || flatChilds.Any(c => c.AvaiableDirectSlots == -1);
      }
    }
  }
}