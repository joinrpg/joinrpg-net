using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Interfaces;

public class NotificationsOptions
{
    [Required]
    public string ServiceAccountEmail { get; set; } = null!;

    public Uri BaseDomain => ExtractDomainFromMail(ServiceAccountEmail);

    private static Uri ExtractDomainFromMail(string mail) => new("https://" + mail.Split("@").Skip(1).SingleOrDefault() ?? throw new InvalidOperationException());

    /// <summary>
    /// Вайт лист предназначен для того, чтобы при локальной отладке не отправлялись письма никому лишнему
    /// </summary>
    public string[] EmailWhiteList { get; set; } = [];
}
