using System.Threading.Tasks;
using Mailgun.Messages;
using Mailgun.Service;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Helpers
{
  public class EmailService : IIdentityMessageService
  {
    public Task SendAsync(IdentityMessage identityMessage)
    {
      if (MailGunFacade.Configured)
      {
        return Task.FromResult(0);
      }

      var messageService = new MessageService(MailGunFacade.ApiKey);

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
      
      return messageService.SendMessageAsync(MailGunFacade.ApiDomain, message);
    }
  }
}