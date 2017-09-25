using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel
{
    public class ProjectAccomodation
    {
        [Key]
        public int Id { get; set; }
        public int AccomodationTypeId { get; set; }
        [ForeignKey("AccomodationTypeId")]
        public virtual ProjectAccomodationType ProjectAccomodationType { get; set; }
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }
        [Required]
        public string Name { get; set; }
        public bool IsPlayerSelectable { get; set; } = true;
        public bool IsInfinite { get; set; } = false;
        public int Capacity { get; set; }
        public bool IsAutofilledAccomodation { get; set; } = false;
        public virtual ICollection<Claim> Inhabitants { get; set; }
        [NotMapped]
        public bool HasAvailableAccomodations => _HasAvailableAccomodations();
        private bool _HasAvailableAccomodations()
        {
            return Capacity - (Inhabitants?.Count ?? 0) > 0;
        }

    }
}
