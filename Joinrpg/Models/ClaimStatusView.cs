using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{
  public enum ClaimStatusView
  {
    [Display(Name = "Подана"), UsedImplicitly]
    AddedByUser,
    [Display(Name = "Предложена"), UsedImplicitly]
    AddedByMaster,
    [Display(Name = "Принята"), UsedImplicitly]
    Approved,
    [Display(Name = "Отозвана"), UsedImplicitly]
    DeclinedByUser,
    [Display(Name = "Отклонена"), UsedImplicitly]
    DeclinedByMaster,
    [Display(Name = "Обсуждается"), UsedImplicitly]
    Discussed,
    [Display(Name = "В листе ожидания"), UsedImplicitly]
    OnHold,
    [Display(Name = "Игрок заехал"), UsedImplicitly]
    CheckedIn,
  }

  public static class ClaimStatusViewExtensions
  {

    public static bool CanChangeTo(this ClaimStatusView fromStatus, ClaimStatusView targetStatus)
      => ((Claim.Status)fromStatus).CanChangeTo((Claim.Status)targetStatus);

      public static bool IsActive(this ClaimStatusView status) =>
          ((Claim.Status) status).IsActive();

  }
}
