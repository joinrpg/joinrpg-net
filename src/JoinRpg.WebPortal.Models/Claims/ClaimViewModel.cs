using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Domain.Problems;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.Web.ProjectMasterTools.Subscribe;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models;

public class ClaimViewModel : IEntityWithCommentsViewModel
{
    public int ClaimId { get; set; }
    public int ProjectId { get; set; }

    [DisplayName("Игрок")]
    public User Player { get; set; }

    public UserLinkViewModel? PlayerLink { get; set; }

    [Display(Name = "Статус заявки")]
    public ClaimFullStatusView Status { get; set; }

    public bool IsMyClaim { get; }

    public bool HasMasterAccess { get; }
    public bool CanManageThisClaim { get; }
    public bool CanChangeRooms { get; }
    public bool ProjectActive { get; }
    public IReadOnlyCollection<CommentViewModel> RootComments { get; }

    public int CharacterId { get; }
    public bool HasBlockingOtherClaimsForThisCharacter { get; }
    public int OtherClaimsFromThisPlayerCount { get; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel ParentGroups { get; set; }

    public PlotDisplayViewModel Plot { get; }

    [Display(Name = "Ответственный мастер")]
    public int ResponsibleMasterId { get; set; }

    [Display(Name = "Ответственный мастер"), ReadOnly(true)]
    public User ResponsibleMaster { get; set; }

    [ReadOnly(true)]
    public bool HasOtherApprovedClaim { get; }

    [ReadOnly(true)]
    public IList<JoinSelectListItem> PotentialCharactersToMove { get; }

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

    public required ClaimSubscribeViewModel SubscriptionTooltip { get; set; }

    public ClaimViewModel(ICurrentUserAccessor currentUser,
        Claim claim,
      IReadOnlyCollection<PlotTextDto> plotElements,
      ProjectInfo projectInfo,
      IProblemValidator<Claim> problemValidator,
      Func<string?, string?> externalPaymentUrlFactory,
      ClaimAccommodationViewModel? accommodationModel)
    {
        AllowToSetGroups = projectInfo.AllowToSetGroups;
        ClaimId = claim.ClaimId;
        CommentDiscussionId = claim.CommentDiscussionId;
        RootComments = claim.CommentDiscussion.ToCommentTreeViewModel(currentUser.UserId);
        HasMasterAccess = claim.HasMasterAccess(currentUser);
        CanManageThisClaim = claim.HasAccess(currentUser.UserId,
            Permission.CanManageClaims,
            ExtraAccessReason.ResponsibleMaster);
        CanChangeRooms = claim.HasAccess(currentUser.UserId,
            Permission.CanSetPlayersAccommodations,
            ExtraAccessReason.PlayerOrResponsible);
        IsMyClaim = claim.PlayerUserId == currentUser.UserId;
        Player = claim.Player;
        PlayerLink = UserLinks.Create(claim.Player, ViewMode.Show);
        ProjectId = claim.ProjectId;
        ProjectName = claim.Project.ProjectName;
        Status = new ClaimFullStatusView(claim, AccessArgumentsFactory.Create(claim, currentUser));
        CharacterId = claim.CharacterId;
        CharacterActive = claim.Character.IsActive;
        CharacterAutoCreated = claim.Character.AutoCreated;

        HasBlockingOtherClaimsForThisCharacter = claim.HasOtherClaimsForThisCharacter();
        HasOtherApprovedClaim = claim.Character.ApprovedClaim is not null && claim.Character.ApprovedClaim != claim;
        PotentialCharactersToMove = claim.Project.Characters
            .Where(x => x.CanMoveClaimTo(claim))
            .Select(ToJoinSelectListItem)
            .ToList();
        OtherClaimsFromThisPlayerCount =
            OtherClaimsFromThisPlayerCount =
                claim.IsApproved || claim.Project.Details.EnableManyCharacters
                    ? 0
                    : claim.OtherPendingClaimsForThisPlayer().Count();

        ResponsibleMasterId = claim.ResponsibleMasterUserId;
        ResponsibleMaster = claim.ResponsibleMasterUser;
        Fields = new CustomFieldsViewModel(currentUser.UserId, claim, projectInfo);
        Navigation =
            CharacterNavigationViewModel.FromClaim(claim,
                currentUser.UserId,
                CharacterNavigationPage.Claim);
        Problems = problemValidator.Validate(claim, projectInfo).Select(p => new ProblemViewModel(p)).ToList();
        PlayerDetails = new UserProfileDetailsViewModel(claim.GetUserInfo(), projectInfo);
        ProjectActive = claim.Project.Active;
        CheckInStarted = claim.Project.Details.CheckInProgress;
        CheckInModuleEnabled = claim.Project.Details.EnableCheckInModule;
        Validator = new ClaimCheckInValidator(claim, problemValidator, projectInfo);

        AccommodationEnabled = claim.Project.Details.EnableAccommodation;

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
        ClaimFee = new ClaimFeeViewModel(claim, this, currentUser.UserId, projectInfo, externalPaymentUrlFactory);

        ParentGroups = new CharacterParentGroupsViewModel(claim.Character, claim.HasMasterAccess(currentUser));

        Plot = new PlotDisplayViewModel(plotElements,
            currentUser,
            claim.Character,
            projectInfo);
        AccommodationModel = accommodationModel;
    }

    private static JoinSelectListItem ToJoinSelectListItem(Character x)
    {
        return new JoinSelectListItem()
        {
            Value = x.CharacterId,
            Text = x.CharacterName,
        };
    }



    public int CommentDiscussionId { get; }
    public bool CheckInStarted { get; }
    public bool CheckInModuleEnabled { get; }
    public ClaimCheckInValidator Validator { get; }
    public bool AccommodationEnabled { get; }
    public string ProjectName { get; set; }
    public ClaimAccommodationViewModel? AccommodationModel { get; }
}
