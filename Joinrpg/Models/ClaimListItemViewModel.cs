using System;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class ClaimListItemViewModel
  {
    [Display(Name="Заявка")]
    public string Name { get; set; }

    [Display(Name = "Игрок")]
    public User Player { get; set; }

    [Display (Name="Игра")]
    public string ProjectName { get; set; }

    [Display (Name="Статус")]
    public Claim.Status ClaimStatus { get; set; }

    [Display (Name ="Обновлена")]
    public DateTime? UpdateDate { get; set; }

    [Display (Name = "Ответственный")]
    public User Responsible { get; set; }

    public int ProjectId { get; set; }

    public int ClaimId{ get; set; }

    public static ClaimListItemViewModel FromClaim(Claim claim)
    {
      return new ClaimListItemViewModel()
      {
        ClaimId = claim.ClaimId,
        ClaimStatus = claim.ClaimStatus,
        Name = claim.Name,
        Player = claim.Player,
        ProjectId = claim.ProjectId,
        ProjectName = claim.Name,
        UpdateDate = claim.StatusChangedDate,
        Responsible = claim.ResponsibleMasterUser
      };
    }
  }
}
