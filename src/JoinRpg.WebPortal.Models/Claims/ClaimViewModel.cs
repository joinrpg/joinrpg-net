using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models;

public class ClaimViewModel : ICharacterWithPlayerViewModel, IEntityWithCommentsViewModel
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

    public int? CharacterId { get; }

    [DisplayName("Заявка в группу")]
    public string? GroupName { get; set; }

    public int? CharacterGroupId { get; }
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
    public List<MasterViewModel> Masters { get; }

    [ReadOnly(true)]
    public bool HasOtherApprovedClaim { get; }

    [ReadOnly(true)]
    public IList<JoinSelectListItem> PotentialCharactersToMove { get; }

    public bool HidePlayer => false;

    public bool HasAccess => true;

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
    public bool? CharacterAutoCreated { get; }

    [ReadOnly(true)]
    public bool? CharacterActive { get; }

    public IEnumerable<UserSubscription> Subscriptions { get; set; }

    public UserSubscriptionTooltip SubscriptionTooltip { get; set; }


    public IEnumerable<ProjectAccommodationType> AvailableAccommodationTypes { get; set; }
    public IEnumerable<AccommodationPotentialNeighbors> PotentialNeighbors { get; set; }
    public IEnumerable<AccommodationInvite> IncomingInvite { get; set; }
    public IEnumerable<AccommodationInvite> OutgoingInvite { get; set; }
    public AccommodationRequest AccommodationRequest { get; set; }


    public ClaimViewModel(User currentUser,
      Claim claim,
      IReadOnlyCollection<PlotElement> plotElements,
      IUriService uriService,
      ProjectInfo projectInfo,
      IProblemValidator<Claim> problemValidator,
      Func<string?, string?> externalPaymentUrlFactory,
      IEnumerable<ProjectAccommodationType>? availableAccommodationTypes = null,
      IEnumerable<AccommodationRequest>? accommodationRequests = null,
      IEnumerable<AccommodationPotentialNeighbors>? potentialNeighbors = null,
      IEnumerable<AccommodationInvite>? incomingInvite = null,
      IEnumerable<AccommodationInvite>? outgoingInvite = null)
    {
        ClaimId = claim.ClaimId;
        CommentDiscussionId = claim.CommentDiscussionId;
        RootComments = claim.CommentDiscussion.ToCommentTreeViewModel(currentUser.UserId);
        HasMasterAccess = claim.HasMasterAccess(currentUser.UserId);
        CanManageThisClaim = claim.HasAccess(currentUser.UserId,
            acl => acl.CanManageClaims,
            ExtraAccessReason.ResponsibleMaster);
        CanChangeRooms = claim.HasAccess(currentUser.UserId,
            acl => acl.CanSetPlayersAccommodations,
            ExtraAccessReason.PlayerOrResponsible);
        IsMyClaim = claim.PlayerUserId == currentUser.UserId;
        Player = claim.Player;
        PlayerLink = UserLinks.Create(claim.Player);
        ProjectId = claim.ProjectId;
        ProjectName = claim.Project.ProjectName;
        Status = new ClaimFullStatusView(claim, AccessArgumentsFactory.Create(claim, currentUser.UserId));
        CharacterGroupId = claim.CharacterGroupId;
        GroupName = claim.Group?.CharacterGroupName;
        CharacterId = claim.CharacterId;
        CharacterActive = claim.Character?.IsActive;
        CharacterAutoCreated = claim.Character?.AutoCreated;
        AvailableAccommodationTypes = availableAccommodationTypes?.Where(a =>
          a.IsPlayerSelectable || a.Id == claim.AccommodationRequest?.AccommodationTypeId ||
          claim.HasMasterAccess(currentUser.UserId)).ToList();
        PotentialNeighbors = potentialNeighbors;
        AccommodationRequest = claim.AccommodationRequest;
        IncomingInvite = incomingInvite;
        OutgoingInvite = outgoingInvite;
        HasBlockingOtherClaimsForThisCharacter = claim.HasOtherClaimsForThisCharacter();
        HasOtherApprovedClaim = claim.Character?.ApprovedClaim is not null && claim.Character.ApprovedClaim != claim;
        PotentialCharactersToMove = claim.Project.Characters
            .Where(x => x.IsAvailable && x.IsActive)
            .Where(x => !claim.IsApproved || x.CharacterType != CharacterType.Slot)
            .OrderBy(x => x.CharacterName)
            .Select(ToJoinSelectListItem)
            .ToList();
        OtherClaimsFromThisPlayerCount =
            OtherClaimsFromThisPlayerCount =
                claim.IsApproved || claim.Project.Details.EnableManyCharacters
                    ? 0
                    : claim.OtherPendingClaimsForThisPlayer().Count();
        Masters = claim.Project.GetMasterListViewModel().ToList();

        ResponsibleMasterId = claim.ResponsibleMasterUserId;
        ResponsibleMaster = claim.ResponsibleMasterUser;
        Fields = new CustomFieldsViewModel(currentUser.UserId, claim, projectInfo);
        Navigation =
            CharacterNavigationViewModel.FromClaim(claim,
                currentUser.UserId,
                CharacterNavigationPage.Claim);
        Problems = problemValidator.Validate(claim, projectInfo).Select(p => new ProblemViewModel(p)).ToList();
        PlayerDetails = new UserProfileDetailsViewModel(claim.Player, currentUser);
        ProjectActive = claim.Project.Active;
        CheckInStarted = claim.Project.Details.CheckInProgress;
        CheckInModuleEnabled = claim.Project.Details.EnableCheckInModule;
        Validator = new ClaimCheckInValidator(claim, problemValidator, projectInfo);

        AccommodationEnabled = claim.Project.Details.EnableAccommodation;

        if (claim.HasAccess(currentUser.UserId,
                acl => acl.CanManageMoney, ExtraAccessReason.Player))
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

        if (claim.Character != null)
        {
            ParentGroups = new CharacterParentGroupsViewModel(claim.Character,
                claim.HasMasterAccess(currentUser.UserId));
        }

        if (claim.IsApproved && claim.Character != null)
        {
            var readOnlyList = claim.Character.GetOrderedPlots(plotElements);
            Plot = PlotDisplayViewModel.Published(readOnlyList,
                currentUser.UserId,
                claim.Character,
                uriService);
        }
        else
        {
            Plot = PlotDisplayViewModel.Empty();
        }
    }

    private static JoinSelectListItem ToJoinSelectListItem(Character x)
    {
        return new JoinSelectListItem()
        {
            Value = x.CharacterId,
            Text = x.CharacterName,
        };
    }

    public UserSubscriptionTooltip GetFullSubscriptionTooltip(IEnumerable<CharacterGroup> parents,
    IReadOnlyCollection<UserSubscription> subscriptions, int claimId)
    {
        var claimStatusChangeGroup = "";
        var commentsGroup = "";
        var fieldChangeGroup = "";
        var moneyOperationGroup = "";

        var subscrTooltip = new UserSubscriptionTooltip()
        {
            HasFullParentSubscription = false,
            Tooltip = "",
            IsDirect = false,
            ClaimStatusChange = false,
            Comments = false,
            FieldChange = false,
            MoneyOperation = false,
        };

        subscrTooltip.IsDirect = subscriptions.FirstOrDefault(s => s.ClaimId == claimId) != null;

        foreach (var par in parents)
        {
            foreach (var subscr in subscriptions)
            {
                if (par.CharacterGroupId == subscr.CharacterGroupId &&
                    !(subscrTooltip.ClaimStatusChange && subscrTooltip.Comments &&
                      subscrTooltip.FieldChange && subscrTooltip.MoneyOperation))
                {
                    if (subscrTooltip.ClaimStatusChange && subscrTooltip.Comments &&
                        subscrTooltip.FieldChange && subscrTooltip.MoneyOperation)
                    {
                        break;
                    }
                    if (subscr.ClaimStatusChange && !subscrTooltip.ClaimStatusChange)
                    {
                        subscrTooltip.ClaimStatusChange = true;
                        claimStatusChangeGroup = par.CharacterGroupName;
                    }
                    if (subscr.Comments && !subscrTooltip.Comments)
                    {
                        subscrTooltip.Comments = true;
                        commentsGroup = par.CharacterGroupName;
                    }
                    if (subscr.FieldChange && !subscrTooltip.FieldChange)
                    {
                        subscrTooltip.FieldChange = true;
                        fieldChangeGroup = par.CharacterGroupName;
                    }
                    if (subscr.MoneyOperation && !subscrTooltip.MoneyOperation)
                    {
                        subscrTooltip.MoneyOperation = true;
                        moneyOperationGroup = par.CharacterGroupName;
                    }
                }
            }
        }

        if (subscrTooltip.ClaimStatusChange && subscrTooltip.Comments && subscrTooltip.FieldChange &&
            subscrTooltip.MoneyOperation)
        {
            subscrTooltip.HasFullParentSubscription = true;
        }

        subscrTooltip.Tooltip = GetFullSubscriptionText(subscrTooltip, claimStatusChangeGroup,
          commentsGroup, fieldChangeGroup, moneyOperationGroup);
        return subscrTooltip;
    }

    public string GetFullSubscriptionText(UserSubscriptionTooltip subscrTooltip,
      string claimStatusChangeGroup, string commentsGroup, string fieldChangeGroup,
      string moneyOperationGroup)
    {
        string res;
        if (subscrTooltip.IsDirect || subscrTooltip.HasFullParentSubscription)
        {
            res = "Вы подписаны на эту заявку";
        }
        else if (!(subscrTooltip.ClaimStatusChange || subscrTooltip.Comments ||
                   subscrTooltip.FieldChange || subscrTooltip.MoneyOperation))
        {
            res = "Вы не подписаны на эту заявку";
        }
        else
        {
            res = "Вы не подписаны на эту заявку, но будете получать уведомления в случаях: <br><ul>";

            if (subscrTooltip.ClaimStatusChange)
            {
                res += "<li>Изменение статуса (группа \"" + claimStatusChangeGroup + "\")</li>";
            }
            if (subscrTooltip.Comments)
            {
                res += "<li>Комментарии (группа \"" + commentsGroup + "\")</li>";
            }
            if (subscrTooltip.FieldChange)
            {
                res += "<li>Изменение полей заявки (группа \"" + fieldChangeGroup + "\")</li>";
            }
            if (subscrTooltip.MoneyOperation)
            {
                res += "<li>Финансовые операции (группа \"" + moneyOperationGroup + "\")</li>";
            }

            res += "</ul>";
        }
        return res;
    }

    public int CommentDiscussionId { get; }
    public bool CheckInStarted { get; }
    public bool CheckInModuleEnabled { get; }
    public ClaimCheckInValidator Validator { get; }
    public bool AccommodationEnabled { get; }
    public string ProjectName { get; set; }
}
