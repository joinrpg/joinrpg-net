using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Common.EmailSending.Impl;
public class MailGunOptions
{
    [Required]
    public string? ApiDomain { get; set; }
    [Required]
    public string? ApiKey { get; set; }

    public bool Enabled { get; set; }
}
