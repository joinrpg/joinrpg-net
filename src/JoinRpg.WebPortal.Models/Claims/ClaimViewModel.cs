using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Claims;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models;

public class ClaimViewModel : IEntityWithCommentsViewModel
{
    public int ClaimId => ClaimIdentification.ClaimId;
    public int ProjectId => ClaimIdentification.ProjectId;

    public ClaimIdentification ClaimIdentification { get; }

    [DisplayName("Игрок")]
    public User Player { get; set; }

    public UserLinkViewModel? PlayerLink { get; set; }

    [Display(Name = "Статус заявки")]
    public ClaimFullStatusView Status { get; set; }

    public bool IsMyClaim { get; }

    public bool HasMasterAccess { get; }
    public bool CanManageThisClaim { get; }
    /// <summary>
    /// Можно ли менять проживание — то же правило, что у операций с проживанием, включая поправку на статус заявки
    /// </summary>
    public bool CanChangeRooms { get; }
    public bool ProjectActive { get; }
    public IReadOnlyCollection<CommentViewModel> RootComments { get; }

    public int CharacterId { get; }
    public bool HasBlockingOtherClaimsForThisCharacter { get; }
    public int OtherClaimsFromThisPlayerCount { get; }

    /// <summary>
    /// Восстановить заявку нельзя: у игрока уже принята другая, а проект разрешает только одного
    /// персонажа. То же правило, что проверяет сервер (<see cref="ClaimValidator.EnsureCanRestoreClaim"/>).
    /// </summary>
    public bool RestoreBlockedByOtherApprovedClaim { get; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel ParentGroups { get; set; }

    public PlotDisplayViewModel Plot { get; }

    [Display(Name = "Ответственный мастер")]
    public UserIdentification ResponsibleMasterId { get; set; } = null!;

    [Display(Name = "Ответственный мастер"), ReadOnly(true)]
    public UserLinkViewModel ResponsibleMaster { get; }

    [ReadOnly(true)]
    public bool HasOtherApprovedClaim { get; }

    public CustomFieldsViewModel Fields { get; }

    public CharacterNavigationViewModel Navigation { get; }

    [Display(Name = "Взнос")]
    public ClaimFeeViewModel ClaimFee { get; set; }

    [ReadOnly(true)]
    public IEnumerable<PaymentTypeViewModel> PaymentTypes { get; }

    /// <summary>
    /// Returns true if project is active and there are any payment method available
    /// </summary>
    public bool IsPaymentsEnabled
        => (PaymentTypes?.Any() ?? false) && ProjectActive;

    [ReadOnly(true)]
    public IEnumerable<ProblemViewModel> Problems { get; }

    public UserProfileDetailsViewModel PlayerDetails { get; set; }

    [ReadOnly(true)]
    public bool CharacterAutoCreated { get; }

    [ReadOnly(true)]
    public bool CharacterActive { get; }

    [ReadOnly(true)]
    public bool AllowToSetGroups { get; }

    public bool HasSensitiveDataAccess { get; }
    public bool SensitiveDataRequired { get; }
    public string? PassportData { get; }
    public string? RegistrationAddress { get; }

    /// <param name="claim">
    /// EF-сущность той же заявки. Пока нужна тому, чего нет в агрегате (ADR021, шаг 6):
    /// комментариям, взносу и типам оплаты, полям, ответственному мастеру как <see cref="User"/>,
    /// проживанию.
    /// </param>
    /// <param name="claimInfo">Заявка вместе с персонажем и профилем игрока (ADR021).</param>
    public ClaimViewModel(ICurrentUserAccessor currentUser,
        Claim claim,
      ClaimInfo claimInfo,
      IReadOnlyCollection<PlotTextDto> plotElements,
      IClaimProblemValidator problemValidator,
      Func<string?, string?> externalPaymentUrlFactory,
      ClaimAccommodationViewModel? accommodationModel,
      ILinkRenderer linkRenderer,
      IReadOnlyDictionary<UserIdentification, UserInfoHeader> fieldUsers)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(claimInfo);
        if (claim.GetId() != claimInfo.ClaimId)
        {
            throw new ArgumentException(
                $"Claim entity {claim.GetId()} does not match claim info {claimInfo.ClaimId}", nameof(claimInfo));
        }

        var projectInfo = claimInfo.ProjectInfo;
        var characterInfo = claimInfo.Character;
        var claimData = claimInfo.Claim;
        var playerInfo = claimInfo.Player;

        ClaimIdentification = claimInfo.ClaimId;
        AllowToSetGroups = projectInfo.GroupTree.AllowToSetGroups;
        CommentDiscussionId = claim.CommentDiscussionId;
        RootComments = claim.CommentDiscussion.ToCommentTreeViewModel(currentUser.UserId, projectInfo);
        HasMasterAccess = projectInfo.HasMasterAccess(currentUser);
        CanManageThisClaim = claim.HasAccess(currentUser.UserId,
            Permission.CanManageClaims,
            ExtraAccessReason.ResponsibleMaster);
        CanChangeRooms = claim.CanChangeAccommodation(currentUser.UserIdentificationOrDefault);
        IsMyClaim = claimData.PlayerId == currentUser.UserIdentificationOrDefault;
        Player = claim.Player;
        PlayerLink = new UserLinkViewModel(playerInfo.ToUserInfoHeader());
        ProjectName = projectInfo.ProjectName.Value;
        Status = ClaimStatusBuilders.CreateFullStatus(
            claimData,
            AccessArgumentsFactory.Create(claimInfo, currentUser.UserIdentificationOrDefault));
        CharacterId = characterInfo.Id.CharacterId;
        CharacterActive = characterInfo.IsActive;
        CharacterAutoCreated = characterInfo.AutoCreated;

        HasBlockingOtherClaimsForThisCharacter = claimInfo.ClaimInCharacter.HasOtherClaimsForThisCharacter();
        HasOtherApprovedClaim = characterInfo.ApprovedClaimId is not null && characterInfo.ApprovedClaimId != claimInfo.ClaimId;
        // Заявки «на паузе» не считаются: конкурентом для StrictlyOneCharacter они не являются,
        // вернуть такую заявку в работу при утверждённой другой не даст ClaimValidator.EnsureCanRestoreClaim.
        OtherClaimsFromThisPlayerCount =
                claimData.IsApproved || !projectInfo.ClaimSettings.StrictlyOneCharacter
                    ? 0
                    : playerInfo.ActiveClaims.Count(c => c.ProjectId == projectInfo.ProjectId && c.ClaimId != claimInfo.ClaimId);
        RestoreBlockedByOtherApprovedClaim =
            !ClaimValidator.CanRestoreClaim(new UserClaimInfo(claimInfo.ClaimId, claimData.Status), playerInfo, projectInfo);

        ResponsibleMasterId = claimData.ResponsibleMasterId;
        ResponsibleMaster = new UserLinkViewModel(projectInfo.GetMasterById(claimData.ResponsibleMasterId).UserInfo);
        Fields = new CustomFieldsViewModel(currentUser.UserId, claim, projectInfo, fieldUsers);
        Navigation =
            CharacterNavigationViewModel.FromClaim(characterInfo,
                claimInfo.ClaimId,
                currentUser.UserIdentification,
                CharacterNavigationPage.Claim);
        Problems = problemValidator.Validate(claimInfo).Select(p => new ProblemViewModel(p)).ToList();
        // playerInfo уже прочитан репозиторием одним запросом. Старый claim.GetUserInfo() лез
        // по навигациям EF-сущности игрока (Extra, Auth, Allrpg, ExternalLogins, Claims, ProjectAcls),
        // а на ProjectAcls ещё и дёргал .Project по одному на проект — до 48 догрузок за запрос (#4960).
        PlayerDetails = new UserProfileDetailsViewModel(playerInfo, projectInfo, currentUser);
        ProjectActive = projectInfo.IsActive;
        CheckInStarted = projectInfo.ProjectCheckInSettings.InProgress;
        CheckInModuleEnabled = projectInfo.ProjectCheckInSettings.CheckInModuleEnabled;
        Validator = new ClaimCheckInValidator(claimInfo, problemValidator);

        AccommodationEnabled = projectInfo.AccommodationSettings.Enabled;

        if (claim.HasAccess(currentUser.UserId, Permission.CanManageMoney, ExtraAccessReason.Player))
        {
            // Finance admins can create any payment.
            // User also can create any payment, but it will be moderated
            PaymentTypes = claim.Project.ActivePaymentTypes.Select(pt => new PaymentTypeViewModel(pt));
        }
        else
        {
            // All other masters can create payments only from a user to himself
            PaymentTypes = claim.Project.ActivePaymentTypes
                .Where(pt => pt.UserId == currentUser.UserId)
                .Select(pt => new PaymentTypeViewModel(pt));
        }
        // Тип и комната — из модели панели «Проживание», построенной по снимку заявки и плану (ADR022).
        ClaimFee = new ClaimFeeViewModel(claim, this, currentUser.UserId, projectInfo, externalPaymentUrlFactory,
            accommodationModel);

        ParentGroups = new CharacterParentGroupsViewModel(characterInfo, HasMasterAccess);

        Plot = new PlotDisplayViewModel(plotElements,
            currentUser,
            characterInfo,
            linkRenderer);
        AccommodationModel = accommodationModel;

        SensitiveDataRequired = projectInfo.ProfileRequirementSettings.SensitiveDataRequired;
        HasSensitiveDataAccess = claimData.PlayerAllowedSensitiveData && SensitiveDataRequired;
        if (HasSensitiveDataAccess)
        {
            // Берём из уже загруженного playerInfo, а не по ленивой навигации claim.Player.Extra:
            // репозиторий профиля отдаёт эти поля с тем же значением и без лишнего запроса.
            PassportData = playerInfo.PassportData;
            RegistrationAddress = playerInfo.RegistrationAddress;
        }
    }

    public int CommentDiscussionId { get; }
    public bool CheckInStarted { get; }
    public bool CheckInModuleEnabled { get; }
    public ClaimCheckInValidator Validator { get; }
    public bool AccommodationEnabled { get; }
    public string ProjectName { get; set; }
    public ClaimAccommodationViewModel? AccommodationModel { get; }
}
