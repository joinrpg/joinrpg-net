using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using Mailgun.Core.Messages;
using Mailgun.Messages;
using Mailgun.Service;
using Newtonsoft.Json.Linq;

namespace JoinRpg.Services.Email
{
  public class EmailServiceImpl : IEmailService
  {
    private readonly string _apiDomain;

    private readonly Recipient _joinRpgSender;

    private readonly Lazy<MessageService> _lazyService;
    private MessageService MessageService => _lazyService.Value;

    private readonly IHtmlService _htmlService;
    private readonly IUriService _uriService;

    private Task Send(IMessage message)
    {
      return MessageService.SendMessageAsync(_apiDomain, message);
    }

    public EmailServiceImpl(string apiDomain, string apiKey, IHtmlService htmlService, IUriService uriService)
    {
      _apiDomain = apiDomain;
      _htmlService = htmlService;
      _joinRpgSender = new Recipient()
      {
        DisplayName = "Команда JoinRpg.Ru",
        Email = "support@" + uriService.GetHostName()
      };
      _uriService = uriService;
      _lazyService = new Lazy<MessageService>(() => new MessageService(apiKey));
    }

    private async Task SendEmail(ICollection<User> recepients, string subject, string text, Recipient sender)
    {
      if (!recepients.Any())
      {
        return;
      }
      var html = _htmlService.MarkdownToHtml(new MarkdownString(text));
      var message = new MessageBuilder().AddUsers(recepients)
        .SetSubject(subject)
        .SetFromAddress(new Recipient() {DisplayName = sender.DisplayName, Email = _joinRpgSender.Email})
        .SetReplyToAddress(sender)
        .SetTextBody(text)
        .SetHtmlBody(html)
        .GetMessage();
      message.RecipientVariables =
        JObject.Parse("{" +string.Join(", ", recepients.Select(r => $"\"{r.Email}\":{{\"name\":\"{r.DisplayName}\"}}")) + "}");
      await Send(message);
    }

    private static string GetInitiatorString(ClaimEmailBase model)
    {
      switch (model.InitiatorType)
      {
        case ParcipantType.Nobody:
        return "";
        case ParcipantType.Master:
        return $"мастером {model.Initiator.DisplayName}";
        case ParcipantType.Player:
        return "игроком";
        default:
        throw new ArgumentOutOfRangeException(nameof(model.InitiatorType), model.InitiatorType, null);
      }
    }
    private async Task SendClaimEmail(ClaimEmailBase model, string text)
    {
      var recepients = model.Recepients.Except(new [] {model.Initiator}).ToList();
      if (!recepients.Any())
      {
        return;
      }
      await SendEmail(recepients, $"{model.ProjectName}: {model.Claim.Name}",
        $@"{text}

{model.Text.Contents}

--
{model.Initiator.DisplayName}", model.Initiator.ToRecipient());
    }

    public Task Email(AddCommentEmail model)
    {
      return SendClaimEmail(model,
        $@"
Добрый день, %recipient.name%!

Заявку «{model.Claim.Name}» игрока «{model.Claim.Player.DisplayName}» откомментирована {GetInitiatorString(model)}.
Ссылка на заявку: {_uriService.Get(model.Claim)}");
    }

    public Task Email(ApproveByMasterEmail model)
    {
      return SendClaimEmail(model,
  $@"
Добрый день, %recipient.name%!

Заявка «{model.Claim.Name}» игрока «{model.Claim.Player.DisplayName}» одобрена {GetInitiatorString(model)}.
Ссылка на заявку: {_uriService.Get(model.Claim)}");
    }

    public Task Email(DeclineByMasterEmail model)
    {
      return SendClaimEmail(model,
$@"
Добрый день, %recipient.name%!

Заявка «{model.Claim.Name}» игрока «{model.Claim.Player.DisplayName}» отклонена {GetInitiatorString(model)}.
Ссылка на заявку: {_uriService.Get(model.Claim)}");
    }

    public Task Email(DeclineByPlayerEmail model)
    {
      return SendClaimEmail(model,
       $@"
Добрый день, %recipient.name%!

Заявка «{model.Claim.Name}» игрока «{model.Claim.Player.DisplayName}» отозвана игроком.   
Ссылка на заявку: {_uriService.Get(model.Claim)}");
    }

    public Task Email(NewClaimEmail model)
    {
      return SendClaimEmail(model,
       $@"
Добрый день, %recipient.name%!

Новая заявка «{model.Claim.Name}» от игрока «{model.Claim.Player.DisplayName}».   
Ссылка на заявку: {_uriService.Get(model.Claim)}");
    }
  }

  public static class Exts
  {
    public static IMessageBuilder AddUsers(this IMessageBuilder builder, IEnumerable<User> users)
    {
      foreach (var user in users.WhereNotNull().Distinct())
      {
        builder.AddToRecipient(user.ToRecipient());
      }
      return builder;
    }

    public static Recipient ToRecipient(this User user)
    {
      return new Recipient() {DisplayName = user.DisplayName, Email = user.Email};
    }
  }
}
