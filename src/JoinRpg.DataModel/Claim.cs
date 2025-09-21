using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.DataModel.Finances;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.DataModel;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by LINQ
public class Claim : IProjectEntity, ILinkable, IFieldContainter
{
    public int ClaimId { get; set; }
    public int CharacterId { get; set; }

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
    public DateTime? CheckInDate { get; set; }

    public DateTime CreateDate { get; set; }

    /// <summary>
    /// Contains values of fields for this claim
    /// </summary>
    public string JsonData { get; set; }

    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new HashSet<UserSubscription>();

    public virtual User ResponsibleMasterUser { get; set; }
    public int ResponsibleMasterUserId { get; set; }


    public enum DenialStatus
    {
        Unavailable,
        Refused,
        NotRespond,
        Removed,
        NotSuitable,
        NotImplementable,
    }
    public DenialStatus? ClaimDenialStatus { get; set; }


    /// <summary>
    /// Returns true when claim is pending an action.
    /// </summary>
    /// <remarks>This property is not mapped to database and can not be used in predicates.</remarks>
    // TODO: Implement as extension
    public bool IsPending => ClaimStatus is not Status.DeclinedByMaster and not Status.DeclinedByUser;

    /// <summary>
    /// Returns true when claim is in discussion.
    /// </summary>
    /// <remarks>This property is not mapped to database and can not be used in predicates.</remarks>
    // TODO: Implement as extension
    public bool IsInDiscussion => ClaimStatus is Status.AddedByMaster or Status.AddedByUser or Status.Discussed;

    /// <summary>
    /// Returns true when claim is approved.
    /// </summary>
    /// <remarks>This property is not mapped to database and can not be used in predicates.</remarks>
    // TODO: Implement as extension
    public bool IsApproved => ClaimStatus is Status.Approved or Status.CheckedIn;

    [Obsolete("Use Character.CharacterName")]
    public string Name => Character.CharacterName ?? "заявка";

    public DateTime LastUpdateDateTime { get; set; }


    public int CommentDiscussionId { get; set; }

    public virtual CommentDiscussion CommentDiscussion { get; set; }


    public enum Status
    {
        AddedByUser,
        AddedByMaster,
        Approved,
        DeclinedByUser,
        DeclinedByMaster,
        Discussed,
        OnHold,
        CheckedIn,
    }

    public Status ClaimStatus { get; set; }

    public int? AccommodationRequest_Id { get; set; }

    [ForeignKey(nameof(AccommodationRequest_Id))]
    public virtual AccommodationRequest? AccommodationRequest { get; set; }

    #region Finance

    /// <summary>
    /// Fee to pay by player, manually set by master.
    /// If null (default), actual fee will be automatically selected
    /// from the project's list of payments.
    /// </summary>
    public int? CurrentFee { get; set; }

    /// <summary>
    /// List of finance operations performed by player.
    /// </summary>
    public virtual ICollection<FinanceOperation> FinanceOperations { get; set; }

    /// <summary>
    /// Returns list of approved finance operations.
    /// </summary>
    public IEnumerable<FinanceOperation> ApprovedFinanceOperations
          => FinanceOperations.Where(fo => fo.State == FinanceOperationState.Approved);

    /// <summary>
    /// List of all linked recurrent payments.
    /// </summary>
    public virtual ICollection<RecurrentPayment> RecurrentPayments { get; set; }

    public bool PreferentialFeeUser { get; set; }

    /// <summary>
    /// Used for caching previously calculated total fields fee
    /// </summary>
    [NotMapped]
    public int? FieldsFee { get; set; }

    #endregion

    public DateTimeOffset? LastMasterCommentAt { get; set; }
    [ForeignKey(nameof(LastMasterCommentBy_Id))]
    public User? LastMasterCommentBy { get; set; }
    public int? LastMasterCommentBy_Id { get; set; }

    public DateTimeOffset? LastVisibleMasterCommentAt { get; set; }
    [ForeignKey(nameof(LastVisibleMasterCommentBy_Id))]
    public User? LastVisibleMasterCommentBy { get; set; }
    public int? LastVisibleMasterCommentBy_Id { get; set; }
    public DateTimeOffset? LastPlayerCommentAt { get; set; }

    /// <summary>
    /// Игрок разрешил мастерам видеть паспорт и адрес прописки
    /// </summary>
    public bool PlayerAllowedSenstiveData { get; set; }

    #region ILinkable impl

    LinkType ILinkable.LinkType => LinkType.Claim;

    string ILinkable.Identification => ClaimId.ToString();
    int? ILinkable.ProjectId => ProjectId;

    #endregion

}
