using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel;

/// <summary>
/// Канал отправки рекламы (ADR010 §2). Настройки канала (например, chat id для Telegram)
/// сериализованы в <see cref="SettingsJson"/>; конкретный формат определяется <see cref="Kind"/>.
/// </summary>
public class AdvertisementChannelEntity
{
    [Key]
    public int AdvertisementChannelId { get; set; }

    public string Name { get; set; }

    public int? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    public int Kind { get; set; }

    public string SettingsJson { get; set; }

    public bool IsActive { get; set; }
}
