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
        ICurrentUserAccessor currentUserAccessor,
        UserProfileDetailsBuilder cardBuilder
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
        PlayerDetails = cardBuilder.Build(playerInfo, projectInfo, currentUserAccessor.UserIdentificationOrDefault, currentUserAccessor.IsAdmin);
        Navigation = CharacterNavigationViewModel.FromClaim(characterInfo, claimId, currentUserAccessor.UserIdentification, CharacterNavigationPage.None);

        CanAcceptFee = projectInfo.ProjectFinanceSettings.CanAcceptCash(currentUserAccessor.UserIdentification);
        ClaimId = claimId.ClaimId;
        ProjectId = claimId.ProjectId.Value;
        var responsibleMaster = projectInfo.GetMasterById(claim.ResponsibleMasterId);
        Master = new UserLinkViewModel(responsibleMaster.UserId, responsibleMaster.Name.DisplayName, ViewMode.Show);
        Handouts = [.. plotElements.Select(e => new HandoutListItemViewModel(e))];
        Problems = [.. Validator.Problems.Select(p => new CheckInProblemViewModel(p))];

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
    /// <summary>
    /// Что не даёт зарегистрировать игрока. Сами паспортные данные стойке не показываются — только
    /// факт, что их не хватает.
    /// </summary>
    public IReadOnlyCollection<CheckInProblemViewModel> Problems { get; }
    public IReadOnlyCollection<HandoutListItemViewModel> Handouts { get; }
    public string CurrentUserFullName { get; }
}

/// <summary>
/// Проблема, которая держит регистрацию: незаполненное поле заявки или данные профиля.
/// </summary>
public record class CheckInProblemViewModel(ClaimProblem Problem) : ProblemViewModel(Problem)
{
    public WhoWllFillEnum WhoWillFill { get; } = Problem.PlayerCanFix ? WhoWllFillEnum.Player : WhoWllFillEnum.Master;

    /// <summary>
    /// Что именно не так: название поля или «В профиле нет паспортных данных».
    /// </summary>
    public string Title => Problem is FieldRelatedProblem fieldProblem ? fieldProblem.Field.Name : ProblemTypeText;

    /// <summary>
    /// Уточнение для поля («Поле не заполнено»); у проблем профиля всё сказано в <see cref="Title"/>.
    /// </summary>
    public string? Details => Problem is FieldRelatedProblem ? ProblemTypeText : null;

    public enum WhoWllFillEnum
    {
        [Display(Name = "Мастер")]
        Master,
        [Display(Name = "Игрок")]
        Player,
    }
}
