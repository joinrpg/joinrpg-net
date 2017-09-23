using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Models.Print;


namespace JoinRpg.Web.Models
{
  public class ClaimViewModel : ICharacterWithPlayerViewModel, IEntityWithCommentsViewModel
  {
    public int ClaimId { get; set; }
    public int ProjectId { get; set; }

    [DisplayName("Игрок")]
    public User Player { get; set; }

    [Display(Name = "Статус заявки")]
    public ClaimStatusView Status { get; set; }

    public bool IsMyClaim { get; }

    public bool HasMasterAccess { get; }
    public bool CanManageThisClaim { get; }
    public bool ProjectActive { get; }
    public IReadOnlyCollection<CommentViewModel> RootComments { get; }

    public int? CharacterId { get; }

    [DisplayName("Заявка в группу")]
    public string GroupName { get; set; }

    public int? CharacterGroupId { get; }
    public int OtherClaimsForThisCharacterCount { get; }
    public int OtherClaimsFromThisPlayerCount { get; }

    [Display(Name = "Описание персонажа")]
    public IHtmlString Description { get; set; }

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
    public ClaimFeeViewModel ClaimFee { get; set; }

    [ReadOnly(true)]
    public IEnumerable<PaymentType> PaymentTypes { get; }

    [ReadOnly(true)]
    public IEnumerable<ProblemViewModel> Problems { get; }

    public UserProfileDetailsViewModel PlayerDetails { get; set; }

    [ReadOnly(true)]
    public bool? CharacterActive { get; }

    public IEnumerable<PluginOperationDescriptionViewModel> PrintPlugins { get; }

    public IEnumerable<UserSubscription> Subscriptions { get; set; }

    public UserSubscriptionTooltip SubscriptionTooltip { get; set; }

    public ClaimViewModel(User currentUser, Claim claim,
      IEnumerable<PluginOperationData<IPrintCardPluginOperation>> pluginOperationDatas,
      IReadOnlyCollection<PlotElement> plotElements)
    {
      ClaimId = claim.ClaimId;
      CommentDiscussionId = claim.CommentDiscussionId;
      RootComments = claim.CommentDiscussion.ToCommentTreeViewModel(currentUser.UserId);
      HasMasterAccess = claim.HasMasterAccess(currentUser.UserId);
      CanManageThisClaim = claim.CanManageClaim(currentUser.UserId);
      IsMyClaim = claim.PlayerUserId == currentUser.UserId;
      Player = claim.Player;
      ProjectId = claim.ProjectId;
      Status = (ClaimStatusView) claim.ClaimStatus;
      CharacterGroupId = claim.CharacterGroupId;
      GroupName = claim.Group?.CharacterGroupName;
      CharacterId = claim.CharacterId;
      CharacterActive = claim.Character?.IsActive;
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
      Description = claim.Character?.Description.ToHtmlString();
      Masters =
        claim.Project.GetMasterListViewModel()
          .Union(new MasterListItemViewModel() {Id = "-1", Name = "Нет"});
      ResponsibleMasterId = claim.ResponsibleMasterUserId ?? -1;
      ResponsibleMaster = claim.ResponsibleMasterUser;
      Fields = new CustomFieldsViewModel(currentUser.UserId, claim);
      Navigation =
        CharacterNavigationViewModel.FromClaim(claim, currentUser.UserId,
          CharacterNavigationPage.Claim);
      ClaimFee = new ClaimFeeViewModel(claim, this, currentUser.UserId);
      Problems = claim.GetProblems().Select(p => new ProblemViewModel(p)).ToList();
      PlayerDetails = new UserProfileDetailsViewModel(claim.Player,
        (AccessReason) claim.Player.GetProfileAccess(currentUser));
      PrintPlugins = pluginOperationDatas.Select(PluginOperationDescriptionViewModel.Create);
      ProjectActive = claim.Project.Active;
      CheckInStarted = claim.Project.Details.CheckInProgress;
      CheckInModuleEnabled = claim.Project.Details.EnableCheckInModule;
      Validator = new ClaimCheckInValidator(claim);

      if (claim.PlayerUserId == currentUser.UserId ||
          claim.HasMasterAccess(currentUser.UserId, acl => acl.CanManageMoney))
      {
        //Finance admins can create any payment. User also can create any payment, but it will be moderated
        PaymentTypes = claim.Project.ActivePaymentTypes;
      }
      else
      {
        //All other master can create only payment from user to himself.
        PaymentTypes =
          claim.Project.ActivePaymentTypes.Where(pt => pt.UserId == currentUser.UserId);
      }


      if (claim.Character != null)
      {
        ParentGroups = new CharacterParentGroupsViewModel(claim.Character,
          claim.HasMasterAccess(currentUser.UserId));
      }

      if (claim.IsApproved && claim.Character != null)
      {
        var readOnlyList = claim.Character.GetOrderedPlots(plotElements);
        Plot = PlotDisplayViewModel.Published(readOnlyList, currentUser.UserId, claim.Character);
      }
      else
      {
        Plot = PlotDisplayViewModel.Empty();
      }
    }

    public UserSubscriptionTooltip GetFullSubscriptionTooltip(IEnumerable<CharacterGroup> parents,
      IReadOnlyCollection<UserSubscription> subscriptions, int claimId)
    {
      string claimStatusChangeGroup = "";
      string commentsGroup = "";
      string fieldChangeGroup = "";
      string moneyOperationGroup = "";

      UserSubscriptionTooltip subscrTooltip = new UserSubscriptionTooltip()
      {
        HasFullParentSubscription = false,
        Tooltip = "",
        IsDirect = false,
        ClaimStatusChange = false,
        Comments = false,
        FieldChange = false,
        MoneyOperation = false
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
      string ClaimStatusChangeGroup, string CommentsGroup, string FieldChangeGroup,
      string MoneyOperationGroup)
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
          res += "<li>Изменение статуса (группа \"" + ClaimStatusChangeGroup + "\")</li>";
        }
        if (subscrTooltip.Comments)
        {
          res += "<li>Комментарии (группа \"" + CommentsGroup + "\")</li>";
        }
        if (subscrTooltip.FieldChange)
        {
          res += "<li>Изменение полей заявки (группа \"" + FieldChangeGroup + "\")</li>";
        }
        if (subscrTooltip.MoneyOperation)
        {
          res += "<li>Финансовые операции (группа \"" + MoneyOperationGroup + "\")</li>";
        }

        res += "</ul>";
      }
      return res;
    }

    public int CommentDiscussionId { get; }
    public bool CheckInStarted { get; }
    public bool CheckInModuleEnabled { get; }
    public ClaimCheckInValidator Validator { get; }
  }




    public class ClaimFeeViewModel
    {
        public ClaimFeeViewModel(Claim claim, ClaimViewModel model, int currentUserId)
        {
            FieldsTotalFee = model.Fields.FieldsTotalFee;
            FieldsFee = model.Fields.FieldsFee;
            CurrentTotalFee = claim.ClaimTotalFee(FieldsTotalFee);
            CurrentFee = claim.ClaimCurrentFee(FieldsTotalFee);
            CurrentBalance = claim.ClaimBalance();
            IsFeeAdmin = claim.HasMasterAccess(currentUserId, acl => acl.CanManageMoney);
            ClaimId = claim.ClaimId;
            ProjectId = claim.ProjectId;
            FeeVariants = claim.Project.ProjectFeeSettings.Select(f => f.Fee).Union(CurrentFee).OrderBy(x => x).ToList();

            // Determining payment status
            PaymentStatus = FinanceExtensions.GetClaimPaymentStatus(CurrentTotalFee, CurrentBalance);
        }

        /// <summary>
        /// Basic fee
        /// </summary>
        public int CurrentFee { get; }

        /// <summary>
        /// Fields fee, separated by bound
        /// </summary>
        public Dictionary<FieldBoundToViewModel, int> FieldsFee { get; }

        /// <summary>
        /// Sum of fields fee
        /// </summary>
        public int FieldsTotalFee { get; }
        
        /// <summary>
        /// Sum of basic fee, total fields fee and finance operations
        /// </summary>
        public int CurrentTotalFee { get; }
        
        public int CurrentBalance { get; }

        public ClaimPaymentStatus PaymentStatus { get; }

        public bool IsFeeAdmin { get; }
        public int ClaimId { get; }
        public int ProjectId { get; }
        public IEnumerable<int> FeeVariants { get; }

        public int GetFieldsFee(bool visible)
        {
            int result = 0;
            foreach (FieldBoundToViewModel key in Enum.GetValues(typeof(FieldBoundToViewModel)))
                result += FieldsFee[key];
            return result;
        }
    }
}
