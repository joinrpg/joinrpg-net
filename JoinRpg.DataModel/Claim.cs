using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by LINQ
  public class Claim : IProjectEntity, ILinkable, IFieldContainter
  {
    public int ClaimId { get; set; }
    public int? CharacterId { get; set; }

    public int? CharacterGroupId { get; set; }

    public virtual CharacterGroup Group { get; set; }

    public virtual Character Character { get; set; }

    public int PlayerUserId { get; set; }

    public int ProjectId { get; set; }
    int IOrderableEntity.Id => ClaimId;
    public virtual Project Project { get; set; }

    public virtual User Player { get; set; }

    public DateTime? PlayerAcceptedDate { get; set; }

    public DateTime? PlayerDeclinedDate { get; set; }
    public DateTime? MasterAcceptedDate { get; set; }
    public DateTime? MasterDeclinedDate { get; set; }

    public DateTime CreateDate { get; set; }

    /// <summary>
    /// Contains values of fields for this claim
    /// </summary>
    public string JsonData { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();

    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new HashSet<UserSubscription>();

    public virtual ICollection<ReadCommentWatermark> Watermarks { get; set; }

    public virtual User ResponsibleMasterUser { get; set; }
    public int? ResponsibleMasterUserId { get; set; }

    public bool IsActive => ClaimStatus != Status.DeclinedByMaster && ClaimStatus != Status.DeclinedByUser && ClaimStatus != Status.OnHold;

    public bool IsPending => ClaimStatus != Status.DeclinedByMaster && ClaimStatus != Status.DeclinedByUser;
    public bool IsInDiscussion => ClaimStatus == Status.AddedByMaster || ClaimStatus == Status.AddedByUser || ClaimStatus == Status.Discussed;
    public bool IsApproved => ClaimStatus == Status.Approved;

    //TODO[Localize]
    public string Name => Character?.CharacterName ?? Group?.CharacterGroupName ?? "заявка";

    public DateTime LastUpdateDateTime { get; set; }

    public int? CurrentFee { get; set; }

    public virtual ICollection<FinanceOperation> FinanceOperations { get; set; }

    //TODO[Localize]
    public enum Status
    {
      [Display(Name = "Подана")] AddedByUser,
      [Display(Name = "Предложена")] AddedByMaster,
      [Display(Name = "Принята")] Approved,
      [Display(Name = "Отозвана")] DeclinedByUser,
      [Display(Name = "Отклонена")] DeclinedByMaster,
      [Display(Name = "Обсуждается")] Discussed,
      [Display(Name = "В листе ожидания")] OnHold,
    }

    public Status ClaimStatus { get; set; }

    #region ILinkable impl

    LinkType ILinkable.LinkType => LinkType.Claim;

    string ILinkable.Identification => ClaimId.ToString();
    int? ILinkable.ProjectId => ProjectId;

    #endregion

    #region helper properties

    public IEnumerable<FinanceOperation> ApprovedFinanceOperations
      => FinanceOperations.Where(fo => fo.State == FinanceOperationState.Approved);

    #endregion
  }
}
