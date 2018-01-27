using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
    public class AccommodationRequest
    {
        [Key]
        public int Id { get; set; }
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }
        public int SubjectClaimId { get; set; }
        [NotNull]
        [ForeignKey("SubjectClaimId")]
        public virtual Claim Subject { get; set; }
        public int AccommodationTypeId { get; set; }
        [NotNull]
        [ForeignKey("AccommodationTypeId")]
        public virtual ProjectAccommodationType AccommodationType { get; set; }
        public int AccommodationId { get; set; }
        [NotNull]
        [ForeignKey("AccommodationId")]
        public virtual ProjectAccommodation Accommodation { get; set; }
        public InviteState IsAccepted { get; set; }
        public string Description { get; set; }

        public enum InviteState
        {
            Unanswered,
            Accepted,
            Declined
        }
    }
}
