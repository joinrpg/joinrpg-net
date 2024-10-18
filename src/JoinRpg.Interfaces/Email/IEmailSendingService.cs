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

    string GetRecepientPlaceholderName();
    string GetUserDependentValue(string valueKey);
}
