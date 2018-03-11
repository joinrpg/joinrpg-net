using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.CheckIn
{
  public class SecondRoleViewModel
  {
    public SecondRoleViewModel(Claim claim, IEnumerable<Character> characters, User currentUser)
    {
      Master = claim.ResponsibleMasterUser;
      Navigation = CharacterNavigationViewModel.FromClaim(claim, currentUser.UserId, CharacterNavigationPage.None);
      PlayerDetails = new UserProfileDetailsViewModel(claim.Player, AccessReason.Master);
      ClaimId = claim.ClaimId;
      Characters =
        characters.Select(
          c => new CharacterListItemViewModel()
          {
            Id = c.CharacterId.ToString(),
            Name = c.CharacterName,
            Master = ResponsibleMasterExtensions.GetResponsibleMaster(c)?.GetDisplayName() ?? "нет",
          });
    }

    public SecondRoleViewModel() { } //For submit

    public CharacterNavigationViewModel Navigation { get; }
    public UserProfileDetailsViewModel PlayerDetails { get; }
    [Display(Name = "Ответственный мастер")]
    public User Master { get; }

    public int ClaimId { get; set; }
    public int ProjectId { get; set; }

    [Display(Name="Новая роль")]
    public int CharacterId { get; set; }

    public IEnumerable<CharacterListItemViewModel> Characters { get; set; }

    public class CharacterListItemViewModel
    {
      public string Id { get; set; }
      public string Name { get; set; }
      public string Master { get; set; }
    }
  }
}
