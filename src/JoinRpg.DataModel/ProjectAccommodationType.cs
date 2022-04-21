using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel;

public class ProjectAccommodationType : IProjectEntity
{
    [Key]
    public int Id { get; set; }
    public int ProjectId { get; set; }
    [ForeignKey("ProjectId")]
    public virtual Project Project { get; set; }
    [Required]
    public string Name { get; set; }
    public MarkdownString Description { get; set; }
    [Required]
    public int Cost { get; set; }
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
    public bool IsInfinite { get; set; } = false;
    public bool IsPlayerSelectable { get; set; } = true;
    public bool IsAutoFilledAccommodation { get; set; } = false;

    public virtual ICollection<ProjectAccommodation> ProjectAccommodations { get; set; }
    public virtual ICollection<AccommodationRequest> Desirous { get; set; }
}
