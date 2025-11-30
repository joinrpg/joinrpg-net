namespace JoinRpg.Services.Interfaces.Notification;

public interface IMassProjectEmailService
{
    Task MassMail(ClaimIdentification[] claimIds, MarkdownString body, string subject, bool alsoMailToMasters);
}
