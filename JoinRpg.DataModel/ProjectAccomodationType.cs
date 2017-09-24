using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel
{
  public class ProjectAccomodationType
  {
    [Key]
    public int Id { get; set; }
    public int ProjectId { get; set; }
    [ForeignKey("ProjectId")]
    public virtual Project Project { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public int Cost { get; set; }

    public virtual ICollection<ProjectAccomodation> ProjectAccomodations { get; set; }
  }
}
