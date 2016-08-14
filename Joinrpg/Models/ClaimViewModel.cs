using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.CommonTypes;
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
    public MarkdownViewModel Description { get; set; }

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
  }

  public class ClaimFeeViewModel
  {
    public int CurrentFee { get; set; }
    public int CurrentTotalFee { get; set; }
    public int CurrentBalance { get; set; }
  }
}