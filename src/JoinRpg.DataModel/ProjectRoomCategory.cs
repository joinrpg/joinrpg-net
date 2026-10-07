using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel;

/// <summary>
/// Категория комнат — пул, из которого селятся один или несколько типов проживания (ADR020).
/// Комнаты принадлежат категории, тип проживания ссылается на неё.
/// </summary>
public class ProjectRoomCategory : IProjectEntity
{
    [Key]
    public int Id { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; }

    [Required]
    public string Name { get; set; }

    public virtual ICollection<ProjectAccommodation> Rooms { get; set; }

    public virtual ICollection<ProjectAccommodationType> AccommodationTypes { get; set; }
}
