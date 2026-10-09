using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Print;

namespace JoinRpg.Web.Models.CheckIn;

public class CheckInClaimModel : IProjectIdAware
{
    /// <param name="claimInfo">
    /// Заявка вместе с персонажем и профилем игрока (ADR021). EF-сущность сюда больше не доезжает:
    /// она тянула за собой ленивые навигации (ResponsibleMasterUser), а профиль игрока собирался
    /// по навигациям EF-сущности пользователя.
    /// </param>
    public CheckInClaimModel(
        ClaimInfo claimInfo,
        IReadOnlyCollection<PlotTextDto> plotElements,
        IClaimProblemValidator claimValidator,
        ICurrentUserAccessor currentUserAccessor
        )
    {
        ArgumentNullException.ThrowIfNull(claimInfo);

        var characterInfo = claimInfo.Character;
        var projectInfo = claimInfo.ProjectInfo;
        var claim = claimInfo.Claim;
        var claimId = claimInfo.ClaimId;
        var playerInfo = claimInfo.Player;

        Validator = new ClaimCheckInValidator(claimInfo, claimValidator);
        CheckInTime = claim.CheckInDate;
        ClaimStatus = (ClaimStatusView)claim.Status;
        PlayerDetails = new UserProfileDetailsViewModel(playerInfo, projectInfo, currentUserAccessor);
        Navigation = CharacterNavigationViewModel.FromClaim(characterInfo, claimId, currentUserAccessor.UserIdentification, CharacterNavigationPage.None);

        CanAcceptFee = projectInfo.ProjectFinanceSettings.CanAcceptCash(currentUserAccessor.UserIdentification);
        ClaimId = claimId.ClaimId;
        ProjectId = claimId.ProjectId.Value;
        var responsibleMaster = projectInfo.GetMasterById(claim.ResponsibleMasterId);
        Master = new UserLinkViewModel(responsibleMaster.UserId, responsibleMaster.Name.DisplayName, ViewMode.Show);
        Handouts = [.. plotElements.Select(e => new HandoutListItemViewModel(e))];
        ProblemFields = [.. Validator.FieldProblems.Select(frp => new NotFilledFieldViewModel(frp))];

        CurrentUserFullName = currentUserAccessor.DisplayName.FullName ?? "";
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
    public UserLinkViewModel Master { get; }
    public IReadOnlyCollection<NotFilledFieldViewModel> ProblemFields { get; }
    public IReadOnlyCollection<HandoutListItemViewModel> Handouts { get; }
    public string CurrentUserFullName { get; }
}

public record class NotFilledFieldViewModel(FieldRelatedProblem fieldRelatedProblem) : ProblemViewModel(fieldRelatedProblem)
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
