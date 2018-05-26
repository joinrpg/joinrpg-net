using System.Collections.Generic;
using System.ComponentModel;
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

        public virtual ICollection<Claim> Subjects { get; set; }

        public int AccommodationTypeId { get; set; }
        [NotNull]
        [ForeignKey("AccommodationTypeId")]
        public virtual ProjectAccommodationType AccommodationType { get; set; }

        public int? AccommodationId { get; set; }
        [CanBeNull]
        [ForeignKey("AccommodationId")]
        public virtual ProjectAccommodation Accommodation { get; set; }

        public InviteState IsAccepted { get; set; }

        //TODO[Localize]
        public enum InviteState
        {
            [Description("Не отвечено")]
            Unanswered,
            [Description("Принято")]
            Accepted,
            [Description("Отклонено")]
            Declined,
            [Description("Отменено")]
            Canceled,
        }
    }
}
