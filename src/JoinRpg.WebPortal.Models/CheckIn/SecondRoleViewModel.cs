using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.CheckIn;

public class SecondRoleViewModel
{
    public SecondRoleViewModel(ClaimInfo claimInfo, ICurrentUserAccessor currentUser)
    {
        ArgumentNullException.ThrowIfNull(claimInfo);

        var projectInfo = claimInfo.ProjectInfo;
        // Ответственный заявки — всегда действующий мастер: снять мастера с проекта нельзя, пока
        // за ним числятся заявки (ProjectAccessService.RemoveAccess передаёт их другому).
        Master = new UserLinkViewModel(projectInfo.GetMasterById(claimInfo.Claim.ResponsibleMasterId).UserInfo);
        Navigation = CharacterNavigationViewModel.FromClaim(claimInfo.Character, claimInfo.ClaimId, currentUser.UserIdentification, CharacterNavigationPage.None);
        PlayerDetails = new UserProfileDetailsViewModel(claimInfo.Player, projectInfo, currentUser);
        ClaimId = claimInfo.ClaimId.ClaimId;
        ProjectId = projectInfo.ProjectId.Value;
    }

    public SecondRoleViewModel() { } //For submit

    public CharacterNavigationViewModel Navigation { get; }
    public UserProfileDetailsViewModel PlayerDetails { get; }
    [Display(Name = "Ответственный мастер")]
    public UserLinkViewModel Master { get; }

    public int ClaimId { get; set; }
    public int ProjectId { get; set; }

    [Display(Name = "Новая роль")]
    public CharacterIdentification CharacterId { get; set; }
}
