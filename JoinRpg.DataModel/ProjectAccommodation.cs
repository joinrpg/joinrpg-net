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
        public virtual ICollection<AccommodationRequest> Inhabitants { get; set; }
    }
}
