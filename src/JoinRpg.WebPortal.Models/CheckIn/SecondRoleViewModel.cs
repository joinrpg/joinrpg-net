using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.CheckIn;

public class SecondRoleViewModel
{
    public SecondRoleViewModel(Claim claim, ICurrentUserAccessor currentUser, ProjectInfo projectInfo, UserInfo playerUserInfo)
    {
        Master = claim.ResponsibleMasterUser;
        Navigation = CharacterNavigationViewModel.FromClaim(claim, currentUser.UserIdentification, CharacterNavigationPage.None, projectInfo);
        PlayerDetails = new UserProfileDetailsViewModel(playerUserInfo, projectInfo, currentUser);
        ClaimId = claim.ClaimId;
        ProjectId = projectInfo.ProjectId.Value;
    }

    public SecondRoleViewModel() { } //For submit

    public CharacterNavigationViewModel Navigation { get; }
    public UserProfileDetailsViewModel PlayerDetails { get; }
    [Display(Name = "Ответственный мастер")]
    public User Master { get; }

    public int ClaimId { get; set; }
    public int ProjectId { get; set; }

    [Display(Name = "Новая роль")]
    public CharacterIdentification CharacterId { get; set; }
}
