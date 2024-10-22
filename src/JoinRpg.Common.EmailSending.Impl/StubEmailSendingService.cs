using JoinRpg.DataModel;
using JoinRpg.Interfaces.Email;
using JoinRpg.Markdown;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.EmailSending.Impl;
internal class StubEmailSendingService(ILogger<StubEmailSendingService> logger) : IEmailSendingService
{
    public string GetUserDependentValue(string valueKey) => "%recipient." + valueKey + "%";
    public string GetRecepientPlaceholderName() => GetUserDependentValue(Constants.MailGunName);
    public Task SendEmails(string subject, MarkdownString body, RecepientData sender, IReadOnlyCollection<RecepientData> to)
    {
        logger.LogInformation(@"Sending email {subject} from {sender} to {to}
BODY:
{body}
",
subject,
sender,
string.Join(", ", to.Select(s => s.ToString())),
body.ToPlainText().ToString()
);
        return Task.CompletedTask;
    }
}
