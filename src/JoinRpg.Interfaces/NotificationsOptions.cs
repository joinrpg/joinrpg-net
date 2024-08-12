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

    public Uri BaseDomain => ExtractDomainFromMail(ServiceAccountEmail);

    private Uri ExtractDomainFromMail(string mail)
    {
        return new Uri("https://" + mail.Split("@").Skip(1).SingleOrDefault() ?? throw new InvalidOperationException());
    }
}
