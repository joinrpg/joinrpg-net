using System.ComponentModel.DataAnnotations;
using JoinRpg.Interfaces.Email;

namespace JoinRpg.Interfaces;
public class NotificationsOptions
{
    [Required]
    public string ServiceAccountEmail { get; set; } = null!;

    [Required]
    public string JoinRpgTeamName { get; set; } = null!;

    public RecepientData ServiceRecepient => new(JoinRpgTeamName, ServiceAccountEmail);
}
