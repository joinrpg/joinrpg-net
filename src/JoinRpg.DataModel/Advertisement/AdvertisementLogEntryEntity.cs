using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel;

/// <summary>
/// Факт отправки рекламы (роли/игры) в конкретном канале в рамках расписания.
/// Каналы и расписания (ADR010 §2) пока захардкожены, поэтому ScheduleId хранится
/// как обычный int без FK.
/// </summary>
public class AdvertisementLogEntryEntity
{
    [Key]
    public int AdvertisementLogEntryId { get; set; }

    public int ScheduleId { get; set; }

    public int Method { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; }

    public int? CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int Status { get; set; }

    public DateTimeOffset SentAt { get; set; }
}
