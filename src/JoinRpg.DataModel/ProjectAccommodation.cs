using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace JoinRpg.DataModel;

public class ProjectAccommodation : IProjectEntity
{
    [Key]
    public int Id { get; set; }

    public int RoomCategoryId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(RoomCategoryId))]
    public virtual ProjectRoomCategory RoomCategory { get; set; }

    [JsonIgnore]
    public int ProjectId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; }

    [Required]
    public string Name { get; set; }

    [JsonIgnore]
    public virtual ICollection<AccommodationRequest> Inhabitants { get; set; }
}
