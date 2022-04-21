using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Email;

internal static class EmailSendingServiceHelpers
{
    public static Task SendEmail(this IEmailSendingService emailSendingService,
        string subject,
        MarkdownString body,
        RecepientData sender,
        RecepientData recepient)
        =>
            emailSendingService.SendEmails(subject,
                body,
                sender,
                new[] { recepient, });

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
