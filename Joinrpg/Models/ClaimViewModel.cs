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

namespace JoinRpg.Web.Models
{
  public class ClaimViewModel : ICharacterWithPlayerViewModel
  {
    public int ClaimId { get; set; }
    public int ProjectId { get; set; }
    [DisplayName("Игрок")]
    public User Player { get; set; }
    [Display(Name="Статус заявки")]
    public Claim.Status Status { get; set; }
    public bool IsMyClaim { get; set; }

    public bool HasMasterAccess { get; set; }
    public bool CanManageThisClaim { get; set; }
    public bool ProjectActive { get; set; }
    public IEnumerable<CommentViewModel> Comments { get; set; }

    public int? CharacterId { get; set; }

    [DisplayName("Заявка в группу")]
    public string GroupName { get; set; }

    public int? CharacterGroupId { get; set; }
    public int OtherClaimsForThisCharacterCount { get; set; }
    public int OtherClaimsFromThisPlayerCount { get; set; }

    [Display(Name = "Описание персонажа")]
    public IHtmlString Description { get; set; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel ParentGroups { get; set; }

    public IEnumerable<PlotElementViewModel> Plot { get; set; }

    [Display(Name = "Ответственный мастер")]
    public int ResponsibleMasterId { get; set; }

    [Display(Name = "Ответственный мастер"), ReadOnly(true)]
    public User ResponsibleMaster { get; set; }

    [ReadOnly(true)]
    public IEnumerable<MasterListItemViewModel> Masters { get; set; }

    [ReadOnly(true)]
    public bool HasOtherApprovedClaim { get; set; }

    [ReadOnly(true)]
    public IList<CharacterTreeItem> Data { get; set; }

    public bool HidePlayer => false;

    public bool HasAccess => true;

    public CustomFieldsViewModel Fields { get; set; }

    public CharacterNavigationViewModel Navigation { get; set; }

    [Display(Name="Взнос")]
    public ClaimFeeViewModel ClaimFee { get; set; }

    [ReadOnly(true)]
    public IEnumerable<PaymentType> PaymentTypes { get; set; }

    [ReadOnly(true)]
    public IEnumerable<ProblemViewModel> Problems { get; set; }

    public UserProfileDetailsViewModel PlayerDetails { get; set; }

    [ReadOnly(true)]
    public bool? CharacterActive { get; set; }

    public IEnumerable<PluginOperationDescriptionViewModel> PrintPlugins { get; set; }

    public ClaimViewModel (int currentUserId, Claim claim, IEnumerable<PluginOperationData<IPrintCardPluginOperation>> pluginOperationDatas, IReadOnlyCollection<PlotElement> plotElements)
    {
      ClaimId = claim.ClaimId;
      Comments =
        claim.Comments.Where(comment => comment.ParentCommentId == null)
          .Select(comment => new CommentViewModel(comment, currentUserId)).OrderBy(c => c.CreatedTime);
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
      Data = new CharacterTreeBuilder(claim.Project.RootGroup, claim.HasMasterAccess(currentUserId)).Generate();
      OtherClaimsFromThisPlayerCount = claim.IsApproved ? 0 : claim.OtherPendingClaimsForThisPlayer().Count();
      Description = claim.Character?.Description.ToHtmlString();
      Masters =
        claim.Project.GetMasterListViewModel()
          .Union(new MasterListItemViewModel() {Id = "-1", Name = "Нет"});
      ResponsibleMasterId = claim.ResponsibleMasterUserId ?? -1;
      ResponsibleMaster = claim.ResponsibleMasterUser;
      Fields = new CustomFieldsViewModel(currentUserId, claim);
      Navigation = CharacterNavigationViewModel.FromClaim(claim, currentUserId, CharacterNavigationPage.Claim);
      ClaimFee = new ClaimFeeViewModel()
      {
        CurrentTotalFee = claim.ClaimTotalFee(),
        CurrentBalance = claim.ClaimBalance(),
        CurrentFee = claim.ClaimCurrentFee()
      };
      Problems = claim.GetProblems().Select(p => new ProblemViewModel(p)).ToList();
      PlayerDetails = UserProfileDetailsViewModel.FromUser(claim.Player);
      PrintPlugins = pluginOperationDatas.Select(PluginOperationDescriptionViewModel.Create);


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

      if (claim.IsApproved)
      {
        var readOnlyList = claim.Character.GetOrderedPlots(plotElements);
        Plot =
          // ReSharper disable once PossibleNullReferenceException
          readOnlyList
            .ToViewModels(claim.HasMasterAccess(currentUserId), claim.Character.CharacterId);
      }
      else
      {
        Plot = Enumerable.Empty<PlotElementViewModel>();
      }
    }
  }

  public class ClaimFeeViewModel
  {
    public int CurrentFee { get; set; }
    public int CurrentTotalFee { get; set; }
    public int CurrentBalance { get; set; }
  }
}