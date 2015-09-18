using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using Mailgun.Messages;
using Mailgun.Service;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace JoinRpg.Services.Email
{
  public class EmailServiceImpl : IEmailService
  {
    private readonly string _apiDomain;
    private readonly string _apiKey;

    private static readonly Recipient JoinRpgSender = new Recipient()
    {
      DisplayName = "Команда JoinRpg.Ru",
      Email = "support@dev.joinrpg.ru"
    };

    private static readonly IRazorEngineService Service = RazorEngineService.Create(new TemplateServiceConfiguration()
    {
      TemplateManager = new ResolvePathTemplateManager(new[] {"Views"})
    });

    public EmailServiceImpl(string apiDomain, string apiKey)
    {
      _apiDomain = apiDomain;
      _apiKey = apiKey;
    }

    public Task SendEmail(IEnumerable<User> users, string templateName, dynamic viewBag)
    {

      var result = Service.RunCompile(name: templateName, modelType: null, model: (object) viewBag);

      var messageService = new MessageService(_apiKey);

      var builder = new MessageBuilder();
      foreach (var user in users)
      {
        builder.AddToRecipient(new Recipient() {DisplayName = user.DisplayName, Email = user.Email});
      }
      var message = builder
        .SetSubject(templateName)
        .SetFromAddress(JoinRpgSender)
        .SetTextBody(result)
        .SetHtmlBody(result)
        .GetMessage();

      return messageService.SendMessageAsync(_apiDomain, message);
    }


  }
}
