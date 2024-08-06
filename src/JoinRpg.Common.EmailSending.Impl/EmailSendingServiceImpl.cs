using JoinRpg.DataModel;
using JoinRpg.Interfaces.Email;
using JoinRpg.Markdown;
using Mailgun.Messages;
using Mailgun.Service;

namespace JoinRpg.Common.EmailSending.Impl;

public class EmailSendingServiceImpl(IMailGunConfig config, IHttpClientFactory httpClientFactory) : IEmailSendingService
{
    private const int MaxRecipientsInChunk = 1000;

    private bool EmailEnabled { get; } = !string.IsNullOrWhiteSpace(config.ApiDomain) && !string.IsNullOrWhiteSpace(config.ApiKey);
    private string ApiDomain { get; } = config.ApiDomain;
    private string ServiceEmail { get; } = config.ServiceEmail;
    private MessageService MessageService { get; } = new MessageService(config.ApiKey, httpClientFactory);

    public string GetUserDependentValue(string valueKey) => "%recipient." + valueKey + "%";


    public string GetRecepientPlaceholderName() => GetUserDependentValue(Constants.MailGunName);


    public async Task SendEmails(string subject,
        string body,
        string text,
        RecepientData sender,
        IReadOnlyCollection<RecepientData> to)
    {
        if (!to.Any())
        {
            return;
        }

        foreach (var recepientChunk in to.Chunk(MaxRecipientsInChunk))
        {
            await SendEmailChunkImpl(recepientChunk, subject, text, sender, body);
        }
    }

    public async Task SendEmails(string subject, MarkdownString body, RecepientData sender, IReadOnlyCollection<RecepientData> to)
    {
        if (!to.Any())
        {
            return;
        }

        var html = body.ToHtmlString().ToHtmlString();
        var text = body.ToPlainText().ToString();

        foreach (var recepientChunk in to.Chunk(MaxRecipientsInChunk))
        {
            await SendEmailChunkImpl(recepientChunk, subject, text, sender, html);
        }
    }

    private async Task SendEmailChunkImpl(IReadOnlyCollection<RecepientData> recipients,
        string subject,
        string text,
        RecepientData sender,
        string html)
    {
        var message = new MessageBuilder().AddUsers(recipients)
            .SetSubject(subject)
            .SetFromAddress(new Recipient()
            {
                DisplayName = sender.DisplayName,
                Email = ServiceEmail,
            })
            .SetReplyToAddress(sender.ToMailGunRecepient())
            .SetTextBody(text)
            .SetHtmlBody(html)
            .GetMessage();

        message.Dkim = true;

        message.RecipientVariables = recipients.ToRecipientVariables();
        if (EmailEnabled)
        {
            var response = await MessageService.SendMessageAsync(ApiDomain, message);
            if (!response.IsSuccessStatusCode)
            {
                throw new EmailSendFailedException(
                    $"Failed to send email. Response is {response.StatusCode} {response.ReasonPhrase}");
            }
        }
    }
}
