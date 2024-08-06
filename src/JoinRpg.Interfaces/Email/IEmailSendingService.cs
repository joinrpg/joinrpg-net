using JoinRpg.DataModel;

namespace JoinRpg.Interfaces.Email;

public interface IEmailSendingService
{
    Task SendEmails(string subject,
        MarkdownString body,
        RecepientData sender,
        IReadOnlyCollection<RecepientData> to);

    Task SendEmail(string subject,
        MarkdownString body,
        RecepientData sender,
        RecepientData recepient)
     => SendEmails(subject, body, sender, [recepient,]);

    [Obsolete("Send markdown emails")]
    Task SendEmails(string subject,
        string body,
        string text,
        RecepientData sender,
        IReadOnlyCollection<RecepientData> to);

    string GetRecepientPlaceholderName();
    string GetUserDependentValue(string valueKey);
}
