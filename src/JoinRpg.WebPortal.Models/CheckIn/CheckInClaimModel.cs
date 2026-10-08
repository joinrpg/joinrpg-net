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
    public CheckInClaimModel(ClaimIdentification claimId,
        CharacterInfo characterInfo,
        UserInfo currentUser,
        UserInfo playerInfo,
        IReadOnlyCollection<PlotTextDto> plotElements,
        IClaimProblemValidator claimValidator,
        ICurrentUserAccessor currentUserAccessor
        )
    {
        ArgumentNullException.ThrowIfNull(claimId);
        ArgumentNullException.ThrowIfNull(characterInfo);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(playerInfo);

        // Метаданные проекта берём из агрегата, а не параметром: второй канал тех же данных
        // можно было бы передать несогласованным с тем, к которому привязан сам персонаж.
        var projectInfo = characterInfo.ProjectInfo;

        // Заявка — доменный снимок из агрегата. EF-сущность сюда больше не доезжает: она тянула
        // за собой ленивые навигации (ResponsibleMasterUser), а страница и так уже строится по
        // CharacterInfo (ADR013).
        var claim = characterInfo.GetClaimById(claimId);

        Validator = new ClaimCheckInValidator(
            new ClaimInfo(new ClaimInCharacter(characterInfo, claim), playerInfo),
            claimValidator);
        CheckInTime = claim.CheckInDate;
        ClaimStatus = (ClaimStatusView)claim.Status;
        // playerInfo приходит параметром: claim.GetUserInfo() собирал профиль по ленивым
        // навигациям EF-сущности игрока (Extra, Auth, ExternalLogins, Claims, ProjectAcls).
        PlayerDetails = new UserProfileDetailsViewModel(playerInfo, projectInfo, currentUserAccessor);
        Navigation = CharacterNavigationViewModel.FromClaim(characterInfo, claimId, currentUserAccessor.UserIdentification, CharacterNavigationPage.None);

        CanAcceptFee = projectInfo.ProjectFinanceSettings.CanAcceptCash(currentUserAccessor.UserIdentification);
        ClaimId = claimId.ClaimId;
        ProjectId = claimId.ProjectId.Value;
        var responsibleMaster = projectInfo.GetMasterById(claim.ResponsibleMasterId);
        Master = new UserLinkViewModel(responsibleMaster.UserId, responsibleMaster.Name.DisplayName, ViewMode.Show);
        Handouts = [.. plotElements.Select(e => new HandoutListItemViewModel(e))];
        ProblemFields = [.. Validator.FieldProblems.Select(frp => new NotFilledFieldViewModel(frp))];

        CurrentUserFullName = currentUser.UserFullName.FullName ?? "";
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
