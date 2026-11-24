using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.DomainTypes.Advertisement;

namespace JoinRpg.DataModel;

/// <summary>
/// Расписание отправки рекламы в конкретном канале (ADR010 §2).
/// </summary>
public class AdvertisementScheduleEntity
{
    [Key]
    public int AdvertisementScheduleId { get; set; }

    public int AdvertisementChannelId { get; set; }

    [ForeignKey(nameof(AdvertisementChannelId))]
    public virtual AdvertisementChannelEntity Channel { get; set; }

    public AdvertisementMethod Method { get; set; }

    public int DaysOfWeekMask { get; set; }

    public string? RegionsFilter { get; set; }

    public bool IsActive { get; set; }
}
