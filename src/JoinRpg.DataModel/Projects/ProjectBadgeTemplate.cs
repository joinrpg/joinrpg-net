using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

/// <summary>
/// Шаблон бейджа проекта. Строки бейджа лежат JSON-ом (формат — <c>BadgeLinesJson</c>).
/// </summary>
public class ProjectBadgeTemplate : IProjectEntity, ICreatedUpdatedTrackedForEntity
{
    [Key]
    public int ProjectBadgeTemplateId { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    public int WidthMm { get; set; }

    public int HeightMm { get; set; }

    [MaxLength(1024)]
    public string? BackgroundUrl { get; set; }

    [Required]
    public string LinesJson { get; set; }

    /// <summary>Удалённый шаблон не удаляется из БД, а выключается — его можно восстановить</summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; }
    public int CreatedById { get; set; }

    public DateTime UpdatedAt { get; set; }
    [ForeignKey(nameof(UpdatedById))]
    public virtual User UpdatedBy { get; set; }
    public int UpdatedById { get; set; }

    #region interface implementations

    int IOrderableEntity.Id => ProjectBadgeTemplateId;

    Project IProjectEntity.Project => Project;

    #endregion
}
