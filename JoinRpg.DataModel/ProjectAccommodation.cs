using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel
{
    public class ProjectAccommodation
    {
        [Key]
        public int Id { get; set; }
        public int AccommodationTypeId { get; set; }
        [ForeignKey(nameof(AccommodationTypeId))]
        public virtual ProjectAccommodationType ProjectAccommodationType { get; set; }
        public int ProjectId { get; set; }
        [ForeignKey(nameof(ProjectId))]
        public virtual Project Project { get; set; }
        [Required]
        public string Name { get; set; }
        public bool IsPlayerSelectable { get; set; } = true;
        public bool IsInfinite { get; set; } = false;
        public int Capacity { get; set; }
        public bool IsAutoFilledAccommodation { get; set; } = false;
        public virtual ICollection<Claim> Inhabitants { get; set; }
        [NotMapped]
        public bool HasAvailableAccommodations => _HasAvailableAccommodations();
        private bool _HasAvailableAccommodations() => Capacity - (Inhabitants?.Count ?? 0) > 0;
    }
}
