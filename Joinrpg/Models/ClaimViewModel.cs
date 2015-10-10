using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models
{
  public class ClaimViewModel : ICharacterFieldsViewModel
  {
    public int ClaimId { get; set; }
    public int ProjectId { get; set; }
    public string ClaimName { get; set; }
    public User Player { get; set; }
    public string ProjectName { get; set; }
    public Claim.Status Status { get; set; }
    public bool IsMyClaim { get; set; }

    public bool HasPlayerAccessToCharacter { get; set; }
    public bool HasMasterAccess { get; set; }
    public bool HasApproveRejectClaim { get; set; }

    public IEnumerable<CharacterFieldValue> CharacterFields { get; set; }
    public IEnumerable<Comment> Comments { get; set; }

    [DisplayName("Заявка на персонажа")]
    public string CharacterName { get; set; }

    public int? CharacterId { get; set; }

    [DisplayName("Заявка в групу")]
    public string GroupName { get; set; }

    public int? CharacterGroupId { get; set; }
    public int OtherClaimsForThisCharacterCount { get; set; }
    public int OtherClaimsFromThisPlayerCount { get; set; }

    [Display(Name = "Описание персонажа")]
    public MarkdownString Description { get; set; }

    public IEnumerable<PlotElementViewModel> Plot { get; set; }

    [Display(Name = "Ответственный мастер")]
    public int ResponsibleMasterId { get; set; }

    [ReadOnly(true)]
    public IEnumerable<MasterListItemViewModel> Masters { get; set; }
  }
}