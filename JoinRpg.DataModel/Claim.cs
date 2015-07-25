using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.DataModel
{
  public class Claim
  {
    public int ClaimId { get; set; }
    public int? CharacterId { get; set; }

    public int? CharacterGroupId { get; set; }

    public virtual CharacterGroup Group { get; set; }

    public virtual Character Character { get; set; }

    public int PlayerUserId { get; set; }

    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }

    public User Player { get; set; }

    public DateTime? PlayerAcceptedDate { get; set; }

    public DateTime? PlayerDeclinedDate { get; set; }
    public DateTime? MasterAcceptedDate { get; set; }
    public DateTime? MasterDeclinedDate { get; set; }

    public virtual ICollection<Comment> Comments { get; set; }

    public bool IsActive => MasterDeclinedDate == null && PlayerDeclinedDate == null;
    public bool IsInDiscussion => IsActive && !IsApproved;
    public bool IsApproved => IsActive && PlayerAcceptedDate != null && MasterAcceptedDate != null;

    public string Name => Character?.CharacterName ?? Group?.CharacterGroupName ?? "заявка";

    public DateTime? StatusChangedDate
      =>
        new[] {PlayerAcceptedDate, PlayerDeclinedDate, MasterAcceptedDate, MasterDeclinedDate}.Where(
          date => date != null).Max();

    public enum Status
    {
      [Display(Name = "Подана")]
      AddedByUser,
      [Display(Name = "Предложена")]
      AddedByMaster,
      [Display(Name = "Принята")]
      Approved,
      [Display(Name = "Отозвана")]
      DeclinedByUser,
      [Display(Name = "Отклонена")]
      DeclinedByMaster
    }

    public Status ClaimStatus
    {
      get
      {
        if (MasterDeclinedDate != null)
        {
          return Status.DeclinedByMaster;
        }
        if (PlayerDeclinedDate != null)
        {
          return Status.DeclinedByUser;
        }
        if (IsApproved)
        {
          return Status.Approved;
        }
        if (MasterAcceptedDate != null)
        {
          return Status.AddedByMaster;
        }
        if (PlayerAcceptedDate != null)
        {
          return Status.AddedByUser;
        }
        throw new InvalidOperationException("Unknown claim status");
      }
    }
  }
}
