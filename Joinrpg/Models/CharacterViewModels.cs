using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{

  public abstract class CharacterViewModelBase : GameObjectViewModelBase, IValidatableObject
  {
    [DisplayName("Принимать заявки на этого персонажа")]
    public bool IsAcceptingClaims
    { get; set; } = true;

    [DisplayName("Имя персонажа"), Required]
    public string Name
    { get; set; }
    public override IEnumerable<CharacterGroupListItemViewModel> PossibleParents => Data.ActiveGroups;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (!ParentCharacterGroupIds.Any())
      {
        yield return new ValidationResult("Персонаж должен принадлежать хотя бы к одной группе");
      }
      if (!IsAcceptingClaims && IsHot)
      {
        yield return new ValidationResult("На горячую роль должны приниматься заявки");
      }
    }

    [Display(Name="Всегда скрывать имя игрока"), Required]
    public bool HidePlayerForCharacter { get; set; }

    [Display(Name = "Горячая роль"), Required]
    public bool IsHot { get; set; }
  }
  public class AddCharacterViewModel : CharacterViewModelBase
  {
  }

  public class EditCharacterViewModel : CharacterViewModelBase
  {
    public int CharacterId { get; set; }

    public CharacterFieldsViewModel Fields { get; set; }

    public CharacterNavigationViewModel Navigation { get; set; }

    public EditCharacterViewModel Fill(Character field, int currentUserId)
    {
      Data = CharacterGroupListViewModel.FromProjectAsMaster(field.Project);
      Navigation = CharacterNavigationViewModel.FromCharacter(field, CharacterNavigationPage.Editing,
        currentUserId);
      Fields = new CharacterFieldsViewModel()
      {
        HasMasterAccess = true,
        EditAllowed = true,
        CharacterFields = field.GetPresentFields(),
        HasPlayerAccessToCharacter = false
      };
      return this;
    }
  }

  public enum CharacterNavigationPage
  {
    None,
    Character, Editing, Claim,
    RejectedClaim,
    AddClaim
  }

  public class CharacterNavigationViewModel
  {
    public CharacterNavigationPage Page { get; private set; }
    public bool HasMasterAccess { get; private set; }
    public bool CanEditRoles { get; private set; }

    public bool CanAddClaim { get; private set; }

    public int? ClaimId { get; private set; }
    public int? CharacterId { get; private set; }
    public int ProjectId { get; private set; }

    public string Name { get; private set; }

    public IEnumerable<ClaimListItemViewModel> DiscussedClaims { get; set; }
    public IEnumerable<ClaimListItemViewModel> RejectedClaims { get; set; }

    public static CharacterNavigationViewModel FromCharacter(Character field, CharacterNavigationPage page, int? currentUserId)
    {
      var masterAccess = field.HasMasterAccess(currentUserId);
      int? claimId = null;
      if (currentUserId != null)
      {
        if (field.ApprovedClaim != null)
        {
          if (masterAccess || field.ApprovedClaim.PlayerUserId == currentUserId)
          {
            claimId = field.ApprovedClaim.ClaimId;
          }
        }
        else
        {
          claimId = field.Claims.SingleOrDefault(c => c.PlayerUserId == currentUserId)?.ClaimId;
        }
      }
      var vm = new CharacterNavigationViewModel
      {
        CanAddClaim = field.IsAvailable,
        ClaimId = claimId,
        HasMasterAccess = masterAccess,
        CanEditRoles = field.HasMasterAccess(currentUserId, s => s.CanEditRoles),
        CharacterId = field.CharacterId,
        ProjectId = field.ProjectId,
        Page = page,
        Name = field.CharacterName
      };

      vm.LoadClaims(field);
      return vm;
    }

    private void LoadClaims(Character field)
    {
      RejectedClaims = LoadClaimsWithCondition(field, claim => !claim.IsActive);
      DiscussedClaims = LoadClaimsWithCondition(field, claim => claim.IsInDiscussion);
    }

    private IEnumerable<ClaimListItemViewModel> LoadClaimsWithCondition(Character field, Func<Claim, bool> predicate)
    {
      return HasMasterAccess && field != null
        ? field.Claims.Where(predicate).Select(ClaimListItemViewModel.FromClaim)
        : Enumerable.Empty<ClaimListItemViewModel>();
    }

    public static CharacterNavigationViewModel FromClaim(Claim claim, int currentUserId, CharacterNavigationPage characterNavigationPage)
    {
      var vm = new CharacterNavigationViewModel
      {
        CanAddClaim = false,
        ClaimId = claim.ClaimId,
        HasMasterAccess = claim.HasMasterAccess(currentUserId),
        CharacterId = claim.Character?.CharacterId,
        ProjectId = claim.ProjectId,
        Page = characterNavigationPage,
        Name = claim.GetTarget().Name
      };
      vm.LoadClaims(claim.Character);
      if (vm.RejectedClaims.Any(c => c.ClaimId ==  claim.ClaimId))
      {
        vm.Page = CharacterNavigationPage.RejectedClaim;
      }
      return vm;
    }
  }

}