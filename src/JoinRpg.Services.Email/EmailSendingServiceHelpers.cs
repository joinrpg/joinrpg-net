using JoinRpg.DataModel;
using JoinRpg.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Email;

internal static class EmailSendingServiceHelpers
{
    /// <summary>
    /// Use this method when no additional parameters are needed for users
    /// </summary>
    public static async Task SendEmail(this IEmailSendingService emailSendingService,
        EmailModelBase model,
        string subject,
        string body)
    {
        var projectEmailEnabled = model.GetEmailEnabled();
        if (!projectEmailEnabled)
        {
            return;
        }

        var recipients = model.GetRecipients();

        await emailSendingService.SendEmails(subject,
            new MarkdownString(body),
            model.Initiator.ToRecepientData(),
            recipients.Select(r => r.ToRecepientData()).ToList());
    }
}
