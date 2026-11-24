using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel;

/// <summary>
/// Факт отправки рекламы (роли/игры) в конкретном канале в рамках расписания.
/// Каналы и расписания хранятся в БД (ADR010 §2), <see cref="ScheduleId"/> — FK на
/// <see cref="AdvertisementScheduleEntity"/>.
/// </summary>
public class AdvertisementLogEntryEntity
{
    [Key]
    public int AdvertisementLogEntryId { get; set; }

    public int ScheduleId { get; set; }

    [ForeignKey(nameof(ScheduleId))]
    public virtual AdvertisementScheduleEntity Schedule { get; set; }

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
