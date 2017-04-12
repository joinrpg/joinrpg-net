using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using Mailgun.Core.Messages;
using Mailgun.Messages;
using Mailgun.Service;

namespace JoinRpg.Services.Email
{
  [UsedImplicitly]
  public class EmailServiceImpl : IEmailService
  {
    private const string JoinRpgTeam = "Команда JoinRpg.Ru";
    
    private readonly string _apiDomain;

    private const int MaxRecepientsInChunk = 1000;

    private readonly Recipient _joinRpgSender;

    private readonly Lazy<MessageService> _lazyService;
    private MessageService MessageService => _lazyService.Value;

    private readonly IUriService _uriService;
    private readonly bool _emailEnabled;

    private Task Send(IMessage message)
    {
      return MessageService.SendMessageAsync(_apiDomain, message);
    }

    public EmailServiceImpl(IUriService uriService, IMailGunConfig config)
    {
      _emailEnabled = !string.IsNullOrWhiteSpace(config.ApiDomain) && !string.IsNullOrWhiteSpace(config.ApiKey);
      _apiDomain = config.ApiDomain;
    
      _joinRpgSender = new Recipient()
      {
        DisplayName = JoinRpgTeam,
        Email = "support@" + config.ApiDomain
      };
      _uriService = uriService;
      _lazyService = new Lazy<MessageService>(() => new MessageService(config.ApiKey));
    }

    private Task SendEmail(User recepient, string subject, string text, Recipient sender)
    {
      return SendEmail(new[] {recepient}, subject, text, sender);
    }

    private async Task SendEmail(ICollection<User> recepients, string subject, string text, Recipient sender)
    {
      if (!recepients.Any())
      {
        return;
      }

      var html = new MarkdownString(text).ToHtmlString().ToHtmlString();

      for (var i = 0; i * MaxRecepientsInChunk < recepients.Count; i++)
      {
        await SendEmailChunkImpl(recepients.Skip(i * MaxRecepientsInChunk).Take(MaxRecepientsInChunk).ToList(), subject,
          text, sender, html);
      }
    }

    private async Task SendEmailChunkImpl(IReadOnlyCollection<User> recepients, string subject, string text, Recipient sender, string html)
    {
      var message = new MessageBuilder().AddUsers(recepients)
        .SetSubject(subject)
        .SetFromAddress(new Recipient() {DisplayName = sender.DisplayName, Email = _joinRpgSender.Email})
        .SetReplyToAddress(sender)
        .SetTextBody(text)
        .SetHtmlBody(html)
        .GetMessage();

      message.RecipientVariables = recepients.ToRecepientVariables();
      if (_emailEnabled)
      {
        await Send(message);
      }
    }

    #region Account emails
    public Task Email(RemindPasswordEmail email)
    {
      return SendEmail(email.Recepient, "Восстановление пароля на JoinRpg.Ru",
        $@"Добрый день, %recipient.name%, 

вы (или кто-то, выдающий себя за вас) запросил восстановление пароля на сайте JoinRpg.Ru. 
Если это вы, кликните <a href=""{
          email.CallbackUrl
          }"">вот по этой ссылке</a>, и мы восстановим вам пароль. 
Если вдруг вам пришло такое письмо, а вы не просили восстанавливать пароль, ничего страшного! Просто проигнорируйте его.

--
{JoinRpgTeam}", _joinRpgSender);
    }

    public Task Email(ConfirmEmail email)
    {
      return SendEmail(email.Recepient, "Регистрация на JoinRpg.Ru",
        $@"Здравствуйте, и добро пожаловать на joinrpg.ru!

Пожалуйста, подтвердите свой аккаунт, кликнув <a href=""{
            email.CallbackUrl
            }"">вот по этой ссылке</a>.

Это необходимо для того, чтобы мастера игр, на которые вы заявитесь, могли надежно связываться с вами.

Если вдруг вам пришло такое письмо, а вы нигде не регистрировались, ничего страшного! Просто проигнорируйте его.

--
{JoinRpgTeam}", _joinRpgSender);
    }
    #endregion

    private async Task SendClaimEmail(ClaimEmailModel model, string actionName, string text = "")
    {
      var projectEmailEnabled = model.GetEmailEnabled();
      if (!projectEmailEnabled)
      {
        return;
      }
      var recepients = model.GetRecepients();

      var fields = string.Join("\n\n",
        model.UpdatedFields.Select(updatedField => $@"{updatedField.Field.FieldName}:
{updatedField.DisplayString}"));

      await SendEmail(recepients, $"{model.ProjectName}: {model.Claim.Name}, игрок {model.GetPlayerName()}",
        $@"Добрый день, {MailGunExts.MailGunRecepientName},
Заявка {model.Claim.Name} игрока {model.Claim.Player.DisplayName} {actionName} {model.GetInitiatorString()}
{text}

{fields}
{model.Text.Contents}

{model.Initiator.DisplayName}

Чтобы ответить на комментарий, перейдите на страницу заявки: {_uriService.Get(model.Claim.CommentDiscussion)}
", model.Initiator.ToRecipient());
    }

    public Task Email(AddCommentEmail model) => SendClaimEmail(model, "откомментирована");

    public Task Email(ApproveByMasterEmail model) => SendClaimEmail(model, "одобрена");

    public Task Email(DeclineByMasterEmail model) => SendClaimEmail(model, "отклонена");

    public Task Email(DeclineByPlayerEmail model) => SendClaimEmail(model, "отозвана");

    public Task Email(NewClaimEmail model) => SendClaimEmail(model, "подана");

    public Task Email(RestoreByMasterEmail model) => SendClaimEmail(model, "восстановлена");

    public Task Email(MoveByMasterEmail model)
      =>
        SendClaimEmail(model, "изменена",
          $@"Заявка перенесена {model.GetInitiatorString()} на новую роль «{model.Claim.GetTarget().Name}».");


    public Task Email(ChangeResponsibleMasterEmail model)
     => SendClaimEmail(model, "изменена", "В заявке изменен ответственный мастер.");

    public Task Email(OnHoldByMasterEmail createClaimEmail)
      => SendClaimEmail(createClaimEmail, "изменена", "Заявка поставлена в лист ожидания");

    public async Task Email(ForumEmail model)
    {
      var projectEmailEnabled = model.GetEmailEnabled();
      if (!projectEmailEnabled)
      {
        return;
      }
      var recepients = model.GetRecepients();
      if (!recepients.Any())
      {
        return;
      }

      await SendEmail(recepients, $"{model.ProjectName}: тема на форуме {model.ForumThread.Header}",
        $@"Добрый день, {MailGunExts.MailGunRecepientName},
На форуме появилось новое сообщение: 

{model.Text.Contents}

{model.Initiator.DisplayName}

Чтобы ответить на комментарий, перейдите на страницу обсуждения: {_uriService.Get(model.ForumThread.CommentDiscussion)}
", model.Initiator.ToRecipient());
    }

    public Task Email(FieldsChangedEmail createClaimEmail) => SendClaimEmail(createClaimEmail, "изменена", "изменены поля");

    public Task Email(FinanceOperationEmail model)
    {
      var message = "";

      if (model.FeeChange != 0)
      {
        message += $"\nИзменение взноса: {model.FeeChange}\n";
      }

      if (model.Money > 0)
      {
        message += $"\nОплата денег игроком: {model.Money}\n";
      }

      if (model.Money < 0)
      {
        message += $"\nВозврат денег игроку: {-model.Money}\n";
      }

      return SendClaimEmail(model, "изменена", message);
    }

    public async Task Email(MassEmailModel model)
    {
      if (!model.GetEmailEnabled())
      {
        return;
      }
      var recepients = model.GetRecepients();
      if (!recepients.Any())
      {
        return;
      }

      if (model.Text.Contents == null)
      {
        throw new ArgumentNullException(nameof(model.Text.Contents));
      }

      var body = Regex.Replace(model.Text.Contents, EmailTokens.Name, MailGunExts.MailGunRecepientName,
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

      await SendEmail(recepients, $"{model.ProjectName}: {model.Subject}",
        $@"{body}

{model.Initiator.DisplayName}
", model.Initiator.ToRecipient());
    }

  }

  internal static class Exts { 

    public static string GetInitiatorString(this ClaimEmailModel model)
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

    public static string GetPlayerName(this ClaimEmailModel model)
    {
      return model.Claim.Player.DisplayName;
    }

    public static bool GetEmailEnabled(this EmailModelBase model)
    {
      return !model.ProjectName.Trim().StartsWith("NOEMAIL");
    }

    public static List<User> GetRecepients(this EmailModelBase model)
    {
      return model.Recepients.Where(u => u != null && u.UserId != model.Initiator.UserId).Distinct().ToList();
    }
  }
}
