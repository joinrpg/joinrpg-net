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
    public MarkdownDbValue Description { get; set; }
    [Required]
    public int Cost { get; set; }
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
    //Not implemented yet, do not use
    public bool IsInfinite { get; set; } = false;
    public bool IsPlayerSelectable { get; set; } = true;
    //Not implemented yet, do not use
    public bool IsAutoFilledAccommodation { get; set; } = false;

    /// <summary>Категория комнат, из которой селится этот тип (ADR020)</summary>
    public int RoomCategoryId { get; set; }
    [ForeignKey(nameof(RoomCategoryId))]
    public virtual ProjectRoomCategory RoomCategory { get; set; }

    [Obsolete("Нельзя использовать за пределами Dal.Impl (ADR022). Группы типа — из RoomCategoryPlan.Groups; ссылка заявки на группу — CharacterClaimInfo.AccommodationGroupId.", DiagnosticId = "JOIN001")]
    public virtual ICollection<AccommodationRequest> Desirous { get; set; }
}
