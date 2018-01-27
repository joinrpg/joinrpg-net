using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel
{
    public class ProjectAccommodationType
    {
        [Key]
        public int Id { get; set; }
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        public int Cost { get; set; }
        public int Capacity { get; set; }
        public bool IsInfinite { get; set; } = false;
        public bool IsPlayerSelectable { get; set; } = true;
        public bool IsAutoFilledAccommodation { get; set; } = false;
        [NotMapped]
        public bool HasAvailableAccommodations => _HasAvailableAccommodations();
        private bool _HasAvailableAccommodations()
            => Capacity * ProjectAccommodations.Count - (Desirous?.Count ?? 0) > 0;

        public virtual ICollection<ProjectAccommodation> ProjectAccommodations { get; set; }
        public virtual ICollection<AccommodationRequest> Desirous { get; set; }
    }
}
