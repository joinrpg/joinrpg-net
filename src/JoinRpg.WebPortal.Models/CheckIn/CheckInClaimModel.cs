using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Print;

namespace JoinRpg.Web.Models.CheckIn;

public class CheckInClaimModel : IProjectIdAware
{
    public CheckInClaimModel(Claim claim,
        User currentUser,
        IReadOnlyCollection<PlotTextDto> plotElements,
        IProblemValidator<Claim> claimValidator,
        ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claim);

        ArgumentNullException.ThrowIfNull(currentUser);

        Validator = new ClaimCheckInValidator(claim, claimValidator, projectInfo);
        CheckInTime = claim.CheckInDate;
        ClaimStatus = (ClaimStatusView)claim.ClaimStatus;
        PlayerDetails = new UserProfileDetailsViewModel(claim.GetUserInfo(), projectInfo);
        Navigation = CharacterNavigationViewModel.FromClaim(claim, currentUser.UserId, CharacterNavigationPage.None);

        CanAcceptFee = claim.Project.CanAcceptCash(currentUser.GetId());
        ClaimId = claim.ClaimId;
        ProjectId = claim.ProjectId;
        Master = claim.ResponsibleMasterUser;
        Handouts = [.. plotElements.Select(e => new HandoutListItemViewModel(e))];
        ProblemFields = [.. Validator.FieldProblems.Select(frp => new NotFilledFieldViewModel(frp))];

        CurrentUserFullName = currentUser.FullName;
    }

    public ClaimCheckInValidator Validator { get; }
    [UIHint("EventTime")]
    public DateTime? CheckInTime { get; }
    public ClaimStatusView ClaimStatus { get; }
    public UserProfileDetailsViewModel PlayerDetails { get; }
    public CharacterNavigationViewModel Navigation { get; }
    public bool CanAcceptFee { get; }
    public int ClaimId { get; }
    public int ProjectId { get; }
    [Display(Name = "Ответственный мастер")]
    public User Master { get; }
    public IReadOnlyCollection<NotFilledFieldViewModel> ProblemFields { get; }
    public IReadOnlyCollection<HandoutListItemViewModel> Handouts { get; }
    public string CurrentUserFullName { get; }
}

public class NotFilledFieldViewModel(FieldRelatedProblem fieldRelatedProblem) : ProblemViewModel(fieldRelatedProblem)
{
    public WhoWllFillEnum WhoWillFill { get; } = fieldRelatedProblem.Field.CanPlayerEdit ? WhoWllFillEnum.Player : WhoWllFillEnum.Master;
    public string FieldName { get; } = fieldRelatedProblem.Field.Name;

    public enum WhoWllFillEnum
    {
        [Display(Name = "Мастер")]
        Master,
        [Display(Name = "Игрок")]
        Player,
    }
}
