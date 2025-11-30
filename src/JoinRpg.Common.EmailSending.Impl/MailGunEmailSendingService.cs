using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Email;
using JoinRpg.Markdown;
using Mailgun.Messages;
using Mailgun.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.EmailSending.Impl;

internal class MailGunEmailSendingService(IOptions<MailGunOptions> config, IHttpClientFactory httpClientFactory, IOptions<NotificationsOptions> notificationsOptions) : IEmailSendingService
{
    private MessageService MessageService { get; } = new MessageService(config.Value.ApiKey, httpClientFactory);

    public string GetUserDependentValue(string valueKey) => "%recipient." + valueKey + "%";

    public string GetRecepientPlaceholderName() => GetUserDependentValue(Constants.MailGunName);

    public async Task SendEmails(string subject, MarkdownString body, RecepientData sender, IReadOnlyCollection<RecepientData> to)
    {
        if (to.Count == 0)
        {
            return;
        }

        var html = body.ToHtmlString();
        var text = body.ToPlainTextWithoutHtmlEscape(); // Экранировать HTML в plain text email не нужно

        if (notificationsOptions.Value.EmailWhiteList.Length > 0)
        {
            to = [.. to.Where(t => notificationsOptions.Value.EmailWhiteList.Contains(t.Email))];
        }

        foreach (var recepientChunk in to.Chunk(Mailgun.Constants.MaximumAllowedRecipients))
        {
            await SendEmailChunkImpl(recepientChunk, subject, text, sender, html);
        }
    }

    private async Task SendEmailChunkImpl(IReadOnlyCollection<RecepientData> recipients,
        string subject,
        string text,
        RecepientData sender,
        MarkupString html)
    {
        var message = new MessageBuilder().AddUsers(recipients)
            .SetSubject(subject)
            .SetFromAddress(new Recipient()
            {
                DisplayName = sender.DisplayName,
                Email = notificationsOptions.Value.ServiceAccountEmail,
            })
            .SetReplyToAddress(sender.ToMailGunRecepient())
            .SetTextBody(text)
            .SetHtmlBody(html.Value)
            .GetMessage();

        message.RecipientVariables = recipients.ToRecipientVariables();
        var response = await MessageService.SendMessageAsync(config.Value.ApiDomain, message);
        if (!response.IsSuccessStatusCode)
        {
            throw new EmailSendFailedException(
                $"Failed to send email. Response is {response.StatusCode} {response.ReasonPhrase}");
        }
    }
}
