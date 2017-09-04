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

    private const string changedFieldsKey = "changedFields";

    private readonly string _apiDomain;

    private const int MaxRecipientsInChunk = 1000;

    private readonly Recipient _joinRpgSender;

    private readonly Lazy<MessageService> _lazyService;
    private MessageService MessageService => _lazyService.Value;

    private readonly IUriService _uriService;
    private readonly bool _emailEnabled;

    private async Task Send(IMessage message)
    {
      var response = await MessageService.SendMessageAsync(_apiDomain, message);
      if (!response.IsSuccessStatusCode)
      {
        throw new EmailSendFailedException(
          $"Failed to send email. Response is {response.StatusCode} {response.ReasonPhrase}");
      }
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

    private Task SendEmail(
      User recipient,
      string subject,
      string text,
      Recipient sender)
    {
      return SendEmail(
        new[] { new MailRecipient(recipient) },
        subject, sender, new MarkdownString(text));
    }

    /// <summary>
    /// Use this method when no additional parameters are needed for users
    /// </summary>
    private async Task SendEmail(
      ICollection<User> recipients,
      string subject,
      string text,
      Recipient sender)
    {
      await SendEmail(recipients.Select(r => new MailRecipient(r)).ToList(), subject, sender, new MarkdownString(text));
    }

      private async Task SendEmail(
          ICollection<MailRecipient> recipients,
          string subject,
          Recipient sender,
          MarkdownString markdownString)
      {
          if (!recipients.Any())
          {
              return;
          }

          var html = markdownString.ToHtmlString().ToHtmlString();
          var text = markdownString.ToPlainText().ToString();

          for (var i = 0; i * MaxRecipientsInChunk < recipients.Count; i++)
          {
              await SendEmailChunkImpl(
                  recipients.Skip(i * MaxRecipientsInChunk).Take(MaxRecipientsInChunk).ToList(),
                  subject, text, sender, html);
          }
      }

      private async Task SendEmailChunkImpl(IReadOnlyCollection<MailRecipient> recipients,
          string subject,
          string text,
          Recipient sender,
          string html)
      {
          var message = new MessageBuilder().AddUsers(recipients)
              .SetSubject(subject)
              .SetFromAddress(new Recipient()
              {
                  DisplayName = sender.DisplayName,
                  Email = _joinRpgSender.Email
              })
              .SetReplyToAddress(sender)
              .SetTextBody(text)
              .SetHtmlBody(html)
              .GetMessage();

          message.RecipientVariables = recipients.ToRecipientVariables();
          if (_emailEnabled)
          {
              await Send(message);
          }
      }

      #region Account emails
    public Task Email(RemindPasswordEmail email)
    {
      return SendEmail(email.Recipient, "Восстановление пароля на JoinRpg.Ru",
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
      return SendEmail(email.Recipient, "Регистрация на JoinRpg.Ru",
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

    /// <summary>
    /// Gets info about changed fields and other attributes for particular user (if available).
    /// </summary>
    private string GetChangedFieldsInfoForUser(
      [NotNull]EmailModelBase model,
      [NotNull]User user)
    {
      IEmailWithUpdatedFieldsInfo mailWithFields = model as IEmailWithUpdatedFieldsInfo;
      if (mailWithFields == null)
      {
        return "";
      }
      //Add project fields that user has right to view
      Predicate<FieldWithValue> accessRightsPredicate =
        CustomFieldsExtensions.GetShowForUserPredicate(mailWithFields.FieldsContainer, user.UserId);
      IEnumerable<MarkdownString> fieldString = mailWithFields
        .UpdatedFields
        .Where(f => accessRightsPredicate(f))
        .Select(updatedField => 
          new MarkdownString(
            $@"__**{updatedField.Field.FieldName}:**__
{MarkDownHelper.HighlightDiffPlaceholder(updatedField.DisplayString, updatedField.PreviousDisplayString).Contents}"));

      //Add info about other changed atttributes (no access rights validation)
      IEnumerable<MarkdownString> otherAttributesStrings = mailWithFields
        .OtherChangedAttributes
        .Select(changedAttribute => new MarkdownString(
          $@"__**{changedAttribute.Key}:**__
{MarkDownHelper.HighlightDiffPlaceholder(changedAttribute.Value.DisplayString, changedAttribute.Value.PreviousDisplayString).Contents}"));

      return string.Join(
        "\n\n", 
        otherAttributesStrings
          .Union(fieldString)
          .Select(x => x.ToHtmlString()));
    }

    private async Task SendClaimEmail([NotNull] ClaimEmailModel model, [NotNull] string actionName, string text = "")
    {
      var projectEmailEnabled = model.GetEmailEnabled();
      if (!projectEmailEnabled)
      {
        return;
      }

      IList<MailRecipient> recipients = model
        .GetRecipients()
        .Select(r => new MailRecipient(
          r,
          new Dictionary<string, string> {{changedFieldsKey, GetChangedFieldsInfoForUser(model, r)}}))
        .ToList();

        string text1 = $@"Добрый день, {MailGunExts.MailGunRecipientName},
Заявка {model.Claim.Name} игрока {model.Claim.Player.DisplayName} {actionName} {model.GetInitiatorString()}
{text}

{MailGunExts.GetUserDependentValue(changedFieldsKey)}
{model.Text.Contents}

{model.Initiator.DisplayName}

Чтобы ответить на комментарий, перейдите на страницу заявки: {_uriService.Get(model.Claim.CommentDiscussion)}
";
        await SendEmail(recipients, $"{model.ProjectName}: {model.Claim.Name}, игрок {model.GetPlayerName()}", model.Initiator.ToRecipient(), new MarkdownString(text1));
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
      var recipients = model.GetRecipients();
      if (!recipients.Any())
      {
        return;
      }

      await SendEmail(recipients, $"{model.ProjectName}: тема на форуме {model.ForumThread.Header}",
        $@"Добрый день, {MailGunExts.MailGunRecipientName},
На форуме появилось новое сообщение: 

{model.Text.Contents}

{model.Initiator.DisplayName}

Чтобы ответить на комментарий, перейдите на страницу обсуждения: {_uriService.Get(model.ForumThread.CommentDiscussion)}
", model.Initiator.ToRecipient());
    }

    public async Task Email(FieldsChangedEmail model)
    {
      var projectEmailEnabled = model.GetEmailEnabled();
      if (!projectEmailEnabled)
      {
        return;
      }

      IList<MailRecipient> recipients = model
        .GetRecipients()
        .Select(r => new MailRecipient(
          r,
          new Dictionary<string, string> {{changedFieldsKey, GetChangedFieldsInfoForUser(model, r)}}))
        .Where(r => !string.IsNullOrEmpty(r.RecipientSpecificValues[changedFieldsKey]))
        //don't email if no changes are visible to user rights
        .ToList();

      Func<bool, string> target = (forMessageBody) => model.IsCharacterMail
        ? $@"персонаж{(forMessageBody ? "a" : "")}  {model.Character.CharacterName}"
        : $"заявк{(forMessageBody ? "и" : "a")} {model.Claim.Name} {(forMessageBody ? $", игрок {model.Claim.Player.DisplayName}" : "")}";


      string linkString = model.IsCharacterMail
        ? _uriService.Get(model.Character)
        : _uriService.Get(model.Claim);
      if (recipients.Any())
      {
          string text = $@"Добрый день, {MailGunExts.MailGunRecipientName},
Данные {target(true)} были изменены. Новые значения:

{MailGunExts.GetUserDependentValue(changedFieldsKey)}

Для просмотра всех данных перейдите на страницу {(model.IsCharacterMail ? "персонажа" : "заявки")}: {linkString}

{model.Initiator.DisplayName}

";
          await SendEmail(recipients, $"{model.ProjectName}: {target(false)}", model.Initiator.ToRecipient(), new MarkdownString(text));
      }
    }

    public Task Email(CheckedInEmal createClaimEmail) => SendClaimEmail(createClaimEmail, "изменена",
      "Игрок прошел регистрацию на полигоне");

    public Task Email(SecondRoleEmail createClaimEmail) => SendClaimEmail(createClaimEmail, "изменена",
      "Игрок выпущен новой ролью");

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
      var recipients = model.GetRecipients();
      if (!recipients.Any())
      {
        return;
      }

      if (model.Text.Contents == null)
      {
        throw new ArgumentNullException(nameof(model.Text.Contents));
      }

      var body = Regex.Replace(model.Text.Contents, EmailTokens.Name, MailGunExts.MailGunRecipientName,
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

      await SendEmail(recipients, $"{model.ProjectName}: {model.Subject}",
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

    public static List<User> GetRecipients(this EmailModelBase model)
    {
      return model.Recipients.Where(u => u != null && u.UserId != model.Initiator.UserId).Distinct().ToList();
    }
  }
}
