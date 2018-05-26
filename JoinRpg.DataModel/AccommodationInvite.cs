using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
    public class AccommodationInvite
    {
        [Key]
        public int Id { get; set; }
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }
        public int FromClaimId { get; set; }
        [NotNull]
        [ForeignKey("FromClaimId")]
        public virtual Claim From { get; set; }
        public int ToClaimId { get; set; }
        [NotNull]
        [ForeignKey("ToClaimId")]
        public virtual Claim To { get; set; }
        public AccommodationRequest.InviteState IsAccepted { get; set; }
        public ResolveDescription ResolveDescription { get; set; } = ResolveDescription.Unspecified;
    }

    //TODO[Localize]
    public enum ResolveDescription
    {
        [Description("Не указан")]
        Unspecified,
        [Description("Отправлено")]
        Open,
        [Description("Принято игроком")]
        Accepted,
        [Description("Принято автоматически")]
        AcceptedAuto,
        [Description("Принято мастером")]
        AcceptedByMaster,
        [Description("Отколнено игроком")]
        Declined,
        [Description("Отколнено автоматически")]
        DeclinedAuto,
        [Description("Отколнено мастером")]
        DeclinedByMaster,
        [Description("Отколнено принятием другого приглашения")]
        DeclinedWithAcceptOther,
        [Description("Отозвано")]
        Canceled,
        [Description("Связанная заявка отозвана")]
        ClaimCanceled,
    }

    /*
     *
     *
     *  private const string AutomaticDeclineByAcceptOther =
        "Приглашение отклонено автоматически, из-за принятия другого приглашения";

        private const string AutomaticDeclineByAcceptOtherToGroup =
            "Приглашение отклонено автоматически, из-за принятия другого группового приглашения";

        private const string ManualAccept = "Приглашение принято";
        private const string ManualDecline = "Приглашение отклонено";
        private const string ManualCancel = "Приглашение было отозвано";
     */
}
