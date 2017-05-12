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
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.Models.Print;
using JoinRpg.Web.Controllers.Common;


namespace JoinRpg.Web.Models
{
    public class ClaimViewModel : ICharacterWithPlayerViewModel, IEntityWithCommentsViewModel
    {
        public int ClaimId { get; set; }
        public int ProjectId { get; set; }
        [DisplayName("Игрок")]
        public User Player { get; set; }
        [Display(Name="Статус заявки")]
        public Claim.Status Status { get; set; }
        public bool IsMyClaim { get; }

        public bool HasMasterAccess { get; }
        public bool CanManageThisClaim { get; }
        public bool ProjectActive { get; }
        public IReadOnlyCollection<CommentViewModel> RootComments { get; }

        public int? CharacterId { get; }

        [DisplayName("Заявка в группу")]
        public string GroupName { get; set; }

        public int? CharacterGroupId { get;  }
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

        [Display(Name="Взнос")]
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

        public ClaimViewModel (int currentUserId, Claim claim, IEnumerable<PluginOperationData<IPrintCardPluginOperation>> pluginOperationDatas, IReadOnlyCollection<PlotElement> plotElements)
        {
            ClaimId = claim.ClaimId;
            CommentDiscussionId = claim.CommentDiscussionId;
            RootComments = claim.CommentDiscussion.ToCommentTreeViewModel(currentUserId);
            HasMasterAccess = claim.HasMasterAccess(currentUserId);
            CanManageThisClaim = claim.CanManageClaim(currentUserId);
            IsMyClaim = claim.PlayerUserId == currentUserId;
            Player = claim.Player;
            ProjectId = claim.ProjectId;
            Status = claim.ClaimStatus;
            CharacterGroupId = claim.CharacterGroupId;
            GroupName = claim.Group?.CharacterGroupName;
            CharacterId = claim.CharacterId;
            CharacterActive = claim.Character?.IsActive;
            OtherClaimsForThisCharacterCount = claim.IsApproved ? 0 : claim.OtherClaimsForThisCharacter().Count();
            HasOtherApprovedClaim = !claim.IsApproved && claim.OtherClaimsForThisCharacter().Any(c => c.IsApproved);
            Data = new CharacterTreeBuilder(claim.Project.RootGroup, currentUserId).Generate();
            OtherClaimsFromThisPlayerCount =
              OtherClaimsFromThisPlayerCount = claim.IsApproved || (claim.Project.Details?.EnableManyCharacters ?? false)
                ? 0
                : claim.OtherPendingClaimsForThisPlayer().Count();
            Description = claim.Character?.Description.ToHtmlString();
            Masters =
              claim.Project.GetMasterListViewModel()
                .Union(new MasterListItemViewModel() {Id = "-1", Name = "Нет"});
            ResponsibleMasterId = claim.ResponsibleMasterUserId ?? -1;
            ResponsibleMaster = claim.ResponsibleMasterUser;
            Fields = new CustomFieldsViewModel(currentUserId, claim);
            Navigation = CharacterNavigationViewModel.FromClaim(claim, currentUserId, CharacterNavigationPage.Claim);
            ClaimFee = new ClaimFeeViewModel(claim, currentUserId);
            Problems = claim.GetProblems().Select(p => new ProblemViewModel(p)).ToList();
            PlayerDetails = UserProfileDetailsViewModel.FromUser(claim.Player);
            PrintPlugins = pluginOperationDatas.Select(PluginOperationDescriptionViewModel.Create);
            ProjectActive = claim.Project.Active;

            if (claim.PlayerUserId == currentUserId || claim.HasMasterAccess(currentUserId, acl => acl.CanManageMoney))
            {
                //Finance admins can create any payment. User also can create any payment, but it will be moderated
                PaymentTypes = claim.Project.ActivePaymentTypes;
            }
            else
            {
                //All other master can create only payment from user to himself.
                PaymentTypes = claim.Project.ActivePaymentTypes.Where(pt => pt.UserId == currentUserId);
            }


            if (claim.Character != null)
            {
                ParentGroups = new CharacterParentGroupsViewModel(claim.Character,
                  claim.HasMasterAccess(currentUserId));
            }

          if (claim.IsApproved && claim.Character != null)
          {
            var readOnlyList = claim.Character.GetOrderedPlots(plotElements);
            Plot = PlotDisplayViewModel.Published(readOnlyList, currentUserId, claim.Character);
          }
          else
          {
            Plot = PlotDisplayViewModel.Empty();
          }
        }

        public UserSubscriptionTooltip GetFullSubscriptionTooltip(IEnumerable<CharacterGroup> parents, IEnumerable<UserSubscription> subscriptions, int ClaimId)
        {
            string ClaimStatusChangeGroup="";
            string CommentsGroup = "";
            string FieldChangeGroup = "";
            string MoneyOperationGroup = "";

            UserSubscriptionTooltip subscrTooltip = new UserSubscriptionTooltip() { HasFullParentSubscription = false,
                                                                                    Tooltip = "",
                                                                                    IsDirect = false,
                                                                                    ClaimStatusChange = false,
                                                                                    Comments = false,
                                                                                    FieldChange = false,
                                                                                    MoneyOperation = false};

            subscrTooltip.IsDirect = subscriptions.FirstOrDefault(s => s.ClaimId == ClaimId) != null ? true : false;

                foreach (var par in parents)
                {
                    foreach (var subscr in subscriptions)
                    {
                        if (par.CharacterGroupId == subscr.CharacterGroupId && !(subscrTooltip.ClaimStatusChange && subscrTooltip.Comments && subscrTooltip.FieldChange && subscrTooltip.MoneyOperation))
                        {
                            if (subscrTooltip.ClaimStatusChange && subscrTooltip.Comments && subscrTooltip.FieldChange && subscrTooltip.MoneyOperation)
                            {
                                break;
                            }
                            if (subscr.ClaimStatusChange && !subscrTooltip.ClaimStatusChange)
                            {
                                subscrTooltip.ClaimStatusChange = true;
                                ClaimStatusChangeGroup = par.CharacterGroupName;
                            }
                            if (subscr.Comments && !subscrTooltip.Comments)
                            {
                                subscrTooltip.Comments = true;
                                CommentsGroup = par.CharacterGroupName;
                            }
                            if (subscr.FieldChange && !subscrTooltip.FieldChange)
                            {
                                subscrTooltip.FieldChange = true;
                                FieldChangeGroup = par.CharacterGroupName;
                            }
                            if (subscr.MoneyOperation && !subscrTooltip.MoneyOperation)
                            {
                                subscrTooltip.MoneyOperation = true;
                                MoneyOperationGroup = par.CharacterGroupName;
                            }
                        }
                    }
                }

                if (subscrTooltip.ClaimStatusChange && subscrTooltip.Comments && subscrTooltip.FieldChange && subscrTooltip.MoneyOperation)
                {
                    subscrTooltip.HasFullParentSubscription = true;
                }

            subscrTooltip.Tooltip = GetFullSubscriptionText(subscrTooltip, ClaimStatusChangeGroup, CommentsGroup, FieldChangeGroup, MoneyOperationGroup);
            return subscrTooltip;
        }

        public string GetFullSubscriptionText(UserSubscriptionTooltip subscrTooltip, string ClaimStatusChangeGroup, string CommentsGroup, string FieldChangeGroup, string MoneyOperationGroup)
        {
            var res = "";
            if (subscrTooltip.IsDirect || subscrTooltip.HasFullParentSubscription)
            {
                res = "Вы подписаны на эту заявку";
            }
            else if (!(subscrTooltip.ClaimStatusChange || subscrTooltip.Comments || subscrTooltip.FieldChange || subscrTooltip.MoneyOperation))
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
        #region Implementation of IEntityWithCommentsViewModel

        public int CommentDiscussionId { get; }

        #endregion
    }
  

  public class ClaimFeeViewModel
  {
    public ClaimFeeViewModel(Claim claim, int currentUserId)
    {
      CurrentTotalFee = claim.ClaimTotalFee();
      CurrentBalance = claim.ClaimBalance();
      CurrentFee = claim.ClaimCurrentFee();
      IsFeeAdmin = claim.HasMasterAccess(currentUserId, acl => acl.CanManageMoney);
      ClaimId = claim.ClaimId;
      ProjectId = claim.ProjectId;
      FeeVariants = claim.Project.ProjectFeeSettings.Select(f => f.Fee).Union(CurrentFee).OrderBy(x => x).ToList();
    }

    public int CurrentFee { get; }
    public int CurrentTotalFee { get; }
    public int CurrentBalance { get; }
    public bool IsFeeAdmin { get; }
    public int ClaimId { get; }
    public int ProjectId { get; }
    public IEnumerable<int> FeeVariants { get; }
  }
}
