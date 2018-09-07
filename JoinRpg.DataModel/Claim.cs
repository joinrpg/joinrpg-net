using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by LINQ
  public class Claim : IProjectEntity, ILinkable, IFieldContainter
  {
    public int ClaimId { get; set; }
    public int? CharacterId { get; set; }

    public int? CharacterGroupId { get; set; }

    [CanBeNull]
    public virtual CharacterGroup Group { get; set; }

    [CanBeNull]
    public virtual Character Character { get; set; }

    public int PlayerUserId { get; set; }

    public int ProjectId { get; set; }
    int IOrderableEntity.Id => ClaimId;
    public virtual Project Project { get; set; }

    [NotNull]
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
    public int? ResponsibleMasterUserId { get; set; }


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


      public bool IsPending =>
            ClaimStatus != Status.DeclinedByMaster
            && ClaimStatus != Status.DeclinedByUser;
    public bool IsInDiscussion =>
            ClaimStatus == Status.AddedByMaster
            || ClaimStatus == Status.AddedByUser
            || ClaimStatus == Status.Discussed;
    public bool IsApproved =>
            ClaimStatus == Status.Approved
            || ClaimStatus == Status.CheckedIn;

    //TODO[Localize]
    public string Name => Character?.CharacterName ?? Group?.CharacterGroupName ?? "заявка";

    public DateTime LastUpdateDateTime { get; set; }


        public int CommentDiscussionId { get; set; }
    [NotNull]
    public virtual CommentDiscussion CommentDiscussion{ get; set; }
       

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
        [CanBeNull]
        public virtual AccommodationRequest AccommodationRequest { get; set; }

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

        public bool PreferentialFeeUser { get; set; }

        /// <summary>
        /// Used for caching previously calculated total fields fee
        /// </summary>
        [NotMapped]
        public int? FieldsFee { get; set; }

        #endregion



        #region ILinkable impl

        LinkType ILinkable.LinkType => LinkType.Claim;

    string ILinkable.Identification => ClaimId.ToString();
    int? ILinkable.ProjectId => ProjectId;

    #endregion

  }
}
