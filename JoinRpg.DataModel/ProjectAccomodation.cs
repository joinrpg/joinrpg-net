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
    [Required]
    public bool IsPlayerSelectable { get; set; } = true;
    [Required]
    public bool IsInfinite { get; set; } = false;
    [Required]
    public int Capacity { get; set; }

    [Required]
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
