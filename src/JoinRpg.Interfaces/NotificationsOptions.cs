using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Interfaces;
public class NotificationsOptions
{
    [Required]
    public string ServiceAccountEmail { get; set; } = null!;
}
