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
        public bool IsGroupInvite { get; set; } = false;
        public string ResolveDescription { get; set; }
    }
}
