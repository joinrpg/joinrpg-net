using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Money;
using JoinRpg.Web.Models.Plot;


namespace JoinRpg.Web.Models
{
    public class ClaimViewModel : ICharacterWithPlayerViewModel, IEntityWithCommentsViewModel
    {
        public int ClaimId { get; set; }
        public int ProjectId { get; set; }

        [DisplayName("Игрок")]
        public User Player { get; set; }

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
        public string GroupName { get; set; }

        public int? CharacterGroupId { get; }
        public int OtherClaimsForThisCharacterCount { get; }
        public int OtherClaimsFromThisPlayerCount { get; }

        [ReadOnly(true), DisplayName("Входит в группы")]
        public CharacterParentGroupsViewModel ParentGroups { get; set; }

        public PlotDisplayViewModel Plot { get; }

        [Display(Name = "Ответственный мастер")]
        public int ResponsibleMasterId { get; set; }

        [Display(Name = "Ответственный мастер"), ReadOnly(true)]
        public User ResponsibleMaster { get; set; }

        [ReadOnly(true)]
        public IEnumerable<MasterListItemViewModel> Masters { get; }

        [ReadOnly(true)]
        public bool HasOtherApprovedClaim { get; }

        [ReadOnly(true)]
        public IList<CharacterTreeItem> Data { get; }

        public bool HidePlayer => false;

        public bool HasAccess => true;

        public CustomFieldsViewModel Fields { get; }

        public CharacterNavigationViewModel Navigation { get; }

        [Display(Name = "Взнос")]
        [NotNull]
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
        public IEnumerable<AccommodationRequest> AccommodationRequests { get; set; }
        public IEnumerable<AccommodationPotentialNeighbors> PotentialNeighbors { get; set; }
        public IEnumerable<AccommodationInvite> IncomingInvite { get; set; }
        public IEnumerable<AccommodationInvite> OutgoingInvite { get; set; }
        public AccommodationRequest AccommodationRequest { get; set; }


        public ClaimViewModel(User currentUser,
          Claim claim,
          IReadOnlyCollection<PlotElement> plotElements,
          IUriService uriService,
          IEnumerable<ProjectAccommodationType> availableAccommodationTypes = null,
          IEnumerable<AccommodationRequest> accommodationRequests = null,
          IEnumerable<AccommodationPotentialNeighbors> potentialNeighbors = null,
          IEnumerable<AccommodationInvite> incomingInvite = null,
          IEnumerable<AccommodationInvite> outgoingInvite = null)
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
            ProjectId = claim.ProjectId;
            ProjectName = claim.Project.ProjectName;
            Status = new ClaimFullStatusView(claim, new AccessArguments(claim, currentUser.UserId));
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
            OtherClaimsForThisCharacterCount = claim.IsApproved
                ? 0
                : claim.OtherClaimsForThisCharacter().Count();
            HasOtherApprovedClaim = !claim.IsApproved &&
                                    claim.OtherClaimsForThisCharacter().Any(c => c.IsApproved);
            Data = new CharacterTreeBuilder(claim.Project.RootGroup, currentUser.UserId).Generate();
            OtherClaimsFromThisPlayerCount =
                OtherClaimsFromThisPlayerCount =
                    claim.IsApproved || claim.Project.Details.EnableManyCharacters
                        ? 0
                        : claim.OtherPendingClaimsForThisPlayer().Count();
            Masters =
                claim.Project.GetMasterListViewModel()
                    .Union(new MasterListItemViewModel() { Id = "-1", Name = "Нет" });
            ResponsibleMasterId = claim.ResponsibleMasterUserId ?? -1;
            ResponsibleMaster = claim.ResponsibleMasterUser;
            Fields = new CustomFieldsViewModel(currentUser.UserId, claim);
            Navigation =
                CharacterNavigationViewModel.FromClaim(claim,
                    currentUser.UserId,
                    CharacterNavigationPage.Claim);
            Problems = claim.GetProblems().Select(p => new ProblemViewModel(p)).ToList();
            PlayerDetails = new UserProfileDetailsViewModel(claim.Player,
                (AccessReason)claim.Player.GetProfileAccess(currentUser));
            ProjectActive = claim.Project.Active;
            CheckInStarted = claim.Project.Details.CheckInProgress;
            CheckInModuleEnabled = claim.Project.Details.EnableCheckInModule;
            Validator = new ClaimCheckInValidator(claim);

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
            ClaimFee = new ClaimFeeViewModel(claim, this, currentUser.UserId);

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


    /// <summary>
    /// Map of <see cref="FinanceOperationType"/> enum
    /// </summary>
    /// <remarks>
    /// Description template parameters:
    /// 0 -- operation type name
    /// 1 -- payment type name (or null)
    /// </remarks>
    public enum FinanceOperationTypeViewModel
    {
        [Display(Name = "Изменение взноса")]
        FeeChange = FinanceOperationType.FeeChange,

        [Display(Name = "Запрос льготы")]
        PreferentialFeeRequest = FinanceOperationType.PreferentialFeeRequest,

        [Display(Name = "Оплата", Description = "{0}: {1}")]
        Submit = FinanceOperationType.Submit,

        [Display(Name = "Оплата онлайн", Description = "{0}")]
        Online = FinanceOperationType.Online,

        [Display(Name = "Возврат", Description = "{0}")]
        Refund = FinanceOperationType.Refund,

        [Display(Name = "Исходящий перевод", Description = "{0} к ")]
        TransferTo = FinanceOperationType.TransferTo,

        [Display(Name = "Входящий перевод", Description = "{0} от ")]
        TransferFrom = FinanceOperationType.TransferFrom,
    }


    public class FinanceOperationViewModel
    {

        public int Id { get; }

        public int ClaimId { get; }

        public string RowCssClass { get; }

        public string Title { get; }

        public int Money { get; }

        public string Description { get; } = "";

        public FinanceOperationTypeViewModel OperationType { get; }

        public FinanceOperationStateViewModel OperationState { get; }

        public string Date { get; }

        public int ProjectId { get; }

        public int? LinkedClaimId { get; }

        public string LinkedClaimName { get; }

        public bool CheckPaymentState { get; }

        public User LinkedClaimUser { get; }

        public bool ShowLinkedClaimLinkIfTransfer { get; }

        public FinanceOperationViewModel(FinanceOperation source, bool isMaster)
        {
            Id = source.CommentId;
            ClaimId = source.ClaimId;
            ProjectId = source.ProjectId;
            Money = source.MoneyAmount;
            LinkedClaimId = source.LinkedClaimId;
            LinkedClaimName = LinkedClaimId.HasValue ? source.LinkedClaim.Name : null;
            LinkedClaimUser = source.LinkedClaim?.Player;
            OperationType = (FinanceOperationTypeViewModel)source.OperationType;
            OperationState = (FinanceOperationStateViewModel)source.State;
            RowCssClass = source.State.ToRowClass();
            Date = source.OperationDate.ToShortDateString();
            ShowLinkedClaimLinkIfTransfer = isMaster;

            Title = OperationType.GetDescription();
            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = OperationType.GetDisplayName();
            }
            else
            {
                Title = string.Format(
                    Title,
                    OperationType.GetDisplayName(),
                    source.PaymentType?.GetDisplayName());
            }

            switch (OperationType)
            {
                case FinanceOperationTypeViewModel.Submit when source.Approved:
                case FinanceOperationTypeViewModel.Submit when source.State == FinanceOperationState.Proposed:
                    Description = OperationState.GetDisplayName();
                    break;
                case FinanceOperationTypeViewModel.Online when source.Approved:
                    Description = OperationState.GetShortNameOrDefault();
                    break;
                case FinanceOperationTypeViewModel.Online when source.State == FinanceOperationState.Proposed:
                    CheckPaymentState = true;
                    break;
            }
        }
    }


    public class ClaimFeeViewModel
    {
        public ClaimFeeViewModel(Claim claim, ClaimViewModel model, int currentUserId)
        {
            Status = model.Status;

            // Reading project fee info applicable for today            
            BaseFeeInfo = claim.CurrentFee == null ? claim.Project.ProjectFeeInfo() : null;
            // Reading base fee of a claim
            BaseFee = claim.BaseFee();
            // Checks for base fee availability
            HasBaseFee = BaseFeeInfo != null || claim.CurrentFee != null;

            AccommodationFee = claim.ClaimAccommodationFee();
            RoomType = claim.AccommodationRequest?.AccommodationType?.Name ?? "";
            RoomName = claim.AccommodationRequest?.Accommodation?.Name;

            FieldsWithFeeCount = model.Fields.FieldWithFeeCount;
            FieldsTotalFee = model.Fields.FieldsTotalFee;

            HasFieldsWithFee = model.Fields.HasFieldsWithFee;
            CurrentTotalFee = claim.ClaimTotalFee(FieldsTotalFee);
            CurrentFee = claim.ClaimCurrentFee(FieldsTotalFee);
            FieldsFee = model.Fields.FieldsFee;

            foreach (FinanceOperationState s in Enum.GetValues(typeof(FinanceOperationState)))
            {
                Balance[s] = 0;
            }

            foreach (var fo in claim.FinanceOperations)
            {
                Balance[fo.State] += fo.MoneyAmount;
            }

            HasMasterAccess = claim.HasMasterAccess(currentUserId);
            HasFeeAdminAccess = claim.HasAccess(currentUserId, acl => acl.CanManageMoney);

            PreferentialFeeEnabled = claim.Project.Details.PreferentialFeeEnabled;
            PreferentialFeeUser = claim.PreferentialFeeUser;
            PreferentialFeeConditions =
                claim.Project.Details.PreferentialFeeConditions.ToHtmlString();
            PreferentialFeeRequestEnabled = PreferentialFeeEnabled && !PreferentialFeeUser && Status.IsActive();

            ClaimId = claim.ClaimId;
            ProjectId = claim.ProjectId;
            FeeVariants = claim.Project.ProjectFeeSettings
                .Select(f => f.Fee)
                .Union(CurrentFee)
                .OrderBy(x => x)
                .ToList();
            FinanceOperations = claim.FinanceOperations.Select(fo => new FinanceOperationViewModel(fo, model.HasMasterAccess));
            VisibleFinanceOperations = FinanceOperations.Where(
                fo => fo.OperationType != FinanceOperationTypeViewModel.FeeChange
                    && fo.OperationType != FinanceOperationTypeViewModel.PreferentialFeeRequest);

            ShowOnlinePaymentControls = model.PaymentTypes.OnlinePaymentsEnabled() && currentUserId == claim.PlayerUserId;
            HasSubmittablePaymentTypes = model.PaymentTypes.Any(pt => pt.TypeKind != PaymentTypeKindViewModel.Online);

            // Determining payment status
            PaymentStatus = FinanceExtensions.GetClaimPaymentStatus(CurrentTotalFee, CurrentBalance);
        }

        /// <summary>
        /// Claim status taken from claim view model
        /// </summary>
        public ClaimFullStatusView Status { get; }

        /// <summary>
        /// Claim fee taken from project settings or defined manually
        /// </summary>
        public int BaseFee { get; }

        /// <summary>
        /// Claim
        /// </summary>
        public ProjectFeeSetting BaseFeeInfo { get; }

        /// <summary>
        /// true if there is any base fee for this claim
        /// </summary>
        public bool HasBaseFee { get; }

        /// <summary>
        /// Sum of fields fees
        /// </summary>
        public int FieldsTotalFee { get; }

        /// <summary>
        /// BaseFee + FieldsTotalFee
        /// </summary>
        public int CurrentFee { get; }

        /// <summary>
        /// Accommodation fee
        /// </summary>
        public int AccommodationFee { get; }

        /// <summary>
        /// Name of choosen room type
        /// </summary>
        public string RoomType { get; }

        /// <summary>
        /// Number or name of occupied room
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        /// Fields fee, separated by bound
        /// </summary>
        public Dictionary<FieldBoundToViewModel, int> FieldsFee { get; }

        /// <summary>
        /// true if fee row should be visible in claim editor.
        /// One of the following conditions has to be met:
        /// CurrentBalance > 0 (player sends some money),
        /// or there is any base fee (assigned manually or automatically),
        /// or there is at least one field with fee
        /// </summary>
        public bool ShowFee
            => CurrentBalance > 0 || HasBaseFee || HasFieldsWithFee;

        /// <summary>
        /// Returns count of fields with assigned fee
        /// </summary>
        public Dictionary<FieldBoundToViewModel, int> FieldsWithFeeCount { get; }

        public bool HasAccommodationFee
            => AccommodationFee != 0;

        /// <summary>
        /// Returns true if there is at least one field with fee
        /// </summary>
        public bool HasFieldsWithFee { get; }

        /// <summary>
        /// Sum of basic fee, total fields fee and finance operations
        /// </summary>
        public int CurrentTotalFee { get; }

        /// <summary>
        /// Sums of all finance operations by type
        /// </summary>
        public readonly Dictionary<FinanceOperationState, int> Balance = new Dictionary<FinanceOperationState, int>();

        /// <summary>
        /// Sum of approved finance operations
        /// </summary>
        public int CurrentBalance => Balance[FinanceOperationState.Approved];

        public ClaimPaymentStatus PaymentStatus { get; }

        /// <summary>
        /// List of associated payment operations
        /// </summary>
        public IEnumerable<FinanceOperationViewModel> FinanceOperations { get; }

        /// <summary>
        /// List of finance operations to be displayed in payments list
        /// </summary>
        public IEnumerable<FinanceOperationViewModel> VisibleFinanceOperations { get; }

        /// <summary>
        /// true if online payment enabled
        /// </summary>
        public bool ShowOnlinePaymentControls { get; }

        /// <summary>
        /// true if there is any payment type(s) except online
        /// </summary>
        public bool HasSubmittablePaymentTypes { get; }

        public bool HasMasterAccess { get; }

        public bool HasFeeAdminAccess { get; }

        public bool PreferentialFeeEnabled { get; }
        public bool PreferentialFeeUser { get; }
        public JoinHtmlString PreferentialFeeConditions { get; }

        /// <summary>
        /// true if a user can request preferential fee
        /// </summary>
        public bool PreferentialFeeRequestEnabled { get; }

        public int ClaimId { get; }
        public int ProjectId { get; }
        public IEnumerable<int> FeeVariants { get; }
    }


    public class FinanceOperationAdminFunctionsViewModel
    {
        public bool Allowed { get; }

        public bool PreferentialFeeEnabled { get; }

        public bool PreferentialFeeUser { get; }

        public FinanceOperationAdminFunctionsViewModel(ClaimFeeViewModel source)
        {
            Allowed = source.HasFeeAdminAccess;
            PreferentialFeeEnabled = source.PreferentialFeeEnabled;
            PreferentialFeeUser = source.PreferentialFeeUser;
        }
    }


    public class FinanceOperationsRowViewModel
    {

        public string HtmlId { get; set; }

        public bool Visible { get; set; }

        public string Title { get; set; }

        public int? Fee { get; set; }

        public string FeeHtmlId { get; set; }

        public int? Payment { get; set; }

        public object AdminFunctions { get; set; }

    }

}
