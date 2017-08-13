using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Services.Interfaces;
using Mailgun.Messages;
using Mailgun.Service;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Helpers
{
  [UsedImplicitly]
  public class EmailService : IIdentityMessageService
  {
    private readonly IMailGunConfig _config;

    public EmailService(IMailGunConfig config)
    {
      _config = config;
    }

    public Task SendAsync(IdentityMessage identityMessage)
    {
      if (string.IsNullOrWhiteSpace(_config.ApiDomain) || string.IsNullOrWhiteSpace(_config.ApiKey))
      {
        return Task.FromResult(0);
      }

      var messageService = new MessageService(_config.ApiKey);

      var message = new MessageBuilder().
        AddToRecipient(new Recipient()
        {
          Email = identityMessage.Destination
        })
        .SetSubject(identityMessage.Subject)
        .SetFromAddress(new Recipient()
        {
          DisplayName = "Команда JoinRpg.Ru",
          Email = "support@dev.joinrpg.ru"
        })
        .SetTextBody(identityMessage.Body)
        .SetHtmlBody(identityMessage.Body)
        .GetMessage();
      
      return messageService.SendMessageAsync(_config.ApiDomain, message);
    }
  }
}