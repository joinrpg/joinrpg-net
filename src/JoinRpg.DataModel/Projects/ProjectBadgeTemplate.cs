using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

/// <summary>
/// Шаблон бейджа проекта. Строки бейджа лежат JSON-ом в <see cref="LinesJson"/>; снаружи с ними
/// работают через <see cref="Lines"/> — формат хранения дальше этого класса не выходит.
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

    /// <summary>Формат хранения строк: строки всегда правятся целиком вместе с шаблоном, отдельной таблицы нет</summary>
    [Required]
    public string LinesJson { get; set; } = "[]";

    [NotMapped]
    public IReadOnlyList<BadgeLine> Lines
    {
        get => JsonSerializer.Deserialize<BadgeLine[]>(LinesJson, LinesJsonOptions) ?? [];
        set => LinesJson = JsonSerializer.Serialize(value, LinesJsonOptions);
    }

    // Кириллицу пишем как есть, а не \uXXXX: так строки читаются в БД глазами. HTML-экранирование
    // здесь не нужно — в разметку значения попадают через Razor, который экранирует сам.
    // Перечисления — строками: читаются в БД и не зависят от порядка значений в enum.
    private static readonly JsonSerializerOptions LinesJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

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
