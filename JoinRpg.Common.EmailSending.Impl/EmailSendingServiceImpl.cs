using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;
using Mailgun.Messages;
using Mailgun.Service;

namespace JoinRpg.Common.EmailSending.Impl
{
    [UsedImplicitly]
    public class EmailSendingServiceImpl : IEmailSendingService
    {
        private bool EmailEnabled { get; }
        private string ApiDomain { get; }
        private string ServiceEmail { get; }
        private MessageService MessageService { get; }

        public EmailSendingServiceImpl(IMailGunConfig config)
        {
            EmailEnabled = !string.IsNullOrWhiteSpace(config.ApiDomain) && !string.IsNullOrWhiteSpace(config.ApiKey);
            ApiDomain = config.ApiDomain;
            ServiceEmail = config.ServiceEmail;

            MessageService = new MessageService(config.ApiKey);
        }

        public string GetUserDependentValue(string valueKey) => "%recipient." + valueKey + "%";

        public string GetRecepientPlaceholderName() => GetUserDependentValue(Constants.MailGunName);


        public async Task SendEmails(string subject, MarkdownString body, RecepientData sender, IReadOnlyCollection<RecepientData> to)
        {
            if (!to.Any())
            {
                return;
            }

            var html = body.ToHtmlString().ToHtmlString();
            var text = body.ToPlainText().ToString();

            for (var i = 0; i * Constants.MaxRecipientsInChunk < to.Count; i++)
            {
                await SendEmailChunkImpl(
                    to.Skip(i * Constants.MaxRecipientsInChunk).Take(Constants.MaxRecipientsInChunk).ToList(),
                    subject, text, sender, html);
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
}
