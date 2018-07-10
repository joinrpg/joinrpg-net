using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Email
{
    [UsedImplicitly]
    public class EmailServiceImpl : IEmailService
    {
        #region general stuff
        private const string JoinRpgTeam = "Команда JoinRpg.Ru";

        private const string ChangedFieldsKey = "changedFields";

        private string StandartGreeting() => $"Добрый день, {RecepientName},\n";

        private readonly RecepientData _joinRpgSender;

        private IEmailSendingService MessageService { get; }

        private readonly IUriService _uriService;

        private string RecepientName => MessageService.GetRecepientPlaceholderName();

        public EmailServiceImpl(IUriService uriService, IMailGunConfig config, IEmailSendingService messageService)
        {
            _joinRpgSender = new RecepientData(JoinRpgTeam, config.ServiceEmail);
            _uriService = uriService;
            MessageService = messageService;
        }

        #endregion

        #region IEmailService implementation
        #region Account emails
        public Task Email(RemindPasswordEmail email)
        {
            string text = $@"Добрый день, %recipient.name%, 

вы (или кто-то, выдающий себя за вас) запросил восстановление пароля на сайте JoinRpg.Ru. 
Если это вы, кликните <a href=""{
                    email.CallbackUrl
                }"">вот по этой ссылке</a>, и мы восстановим вам пароль. 
Если вдруг вам пришло такое письмо, а вы не просили восстанавливать пароль, ничего страшного! Просто проигнорируйте его.

--
{JoinRpgTeam}";
            User recipient = email.Recipient;
            return MessageService.SendEmail("Восстановление пароля на JoinRpg.Ru",
                new MarkdownString(text),
                _joinRpgSender,
                recipient.ToRecepientData());
        }

        public Task Email(ConfirmEmail email)
        {
            string text = $@"Здравствуйте, и добро пожаловать на joinrpg.ru!

Пожалуйста, подтвердите свой аккаунт, кликнув <a href=""{
                    email.CallbackUrl
                }"">вот по этой ссылке</a>.

Это необходимо для того, чтобы мастера игр, на которые вы заявитесь, могли надежно связываться с вами.

Если вдруг вам пришло такое письмо, а вы нигде не регистрировались, ничего страшного! Просто проигнорируйте его.

--
{JoinRpgTeam}";
            return MessageService.SendEmail("Регистрация на JoinRpg.Ru",
                new MarkdownString(text),
                _joinRpgSender,
                email.Recipient.ToRecepientData());
        }
        #endregion

        public Task Email(AddCommentEmail model) => SendClaimEmail(model, "откомментирована");

        public Task Email(ApproveByMasterEmail model) => SendClaimEmail(model);

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
            await MessageService.SendEmail(model, $"{model.ProjectName}: тема на форуме {model.ForumThread.Header}",
                StandartGreeting() + $@"
На форуме появилось новое сообщение: 

{model.Text.Contents}

{model.Initiator.GetDisplayName()}

Чтобы ответить на комментарий, перейдите на страницу обсуждения: {
                        _uriService.Get(model.ForumThread.CommentDiscussion)
                    }
");
        }

        public async Task Email(FieldsChangedEmail model)
        {
            var projectEmailEnabled = model.GetEmailEnabled();
            if (!projectEmailEnabled)
            {
                return;
            }

            var recipients = model
              .GetRecipients()
              .Select(r => new RecepientData(
                r.GetDisplayName(),
                r.Email,
                new Dictionary<string, string> { { ChangedFieldsKey, GetChangedFieldsInfoForUser(model, r) } }))
              .Where(r => !string.IsNullOrEmpty(r.RecipientSpecificValues[ChangedFieldsKey]))
              //don't email if no changes are visible to user rights
              .ToList();

            string Target(bool forMessageBody) => model.IsCharacterMail
                ? $@"персонаж{(forMessageBody ? "a" : "")}  {model.Character.CharacterName}"
                : $"заявк{(forMessageBody ? "и" : "a")} {model.Claim?.Name} {(forMessageBody ? $", игрок {model.Claim?.Player.GetDisplayName()}" : "")}";


            string linkString = _uriService.Get(model.GetLinkable());

            if (recipients.Any())
            {
                string text = $@"{StandartGreeting()},
Данные {Target(true)} были изменены. Новые значения:

{MessageService.GetUserDependentValue(ChangedFieldsKey)}

Для просмотра всех данных перейдите на страницу {(model.IsCharacterMail ? "персонажа" : "заявки")}: {linkString}

{model.Initiator.GetDisplayName()}

";
                //All emails related to claim should have the same title, even if the change was made to a character
                Claim claim = model.IsCharacterMail ? model.Character.ApprovedClaim : model.Claim;

                

                var subject = claim != null
                    ? model.GetClaimEmailTitle(claim)
                    : $"{model.ProjectName}: {Target(false)}";

                await MessageService.SendEmails(subject,
                    new MarkdownString(text),
                    model.Initiator.ToRecepientData(),
                    recipients);
            }
        }

        public async Task Email(UnOccupyRoomEmail email)
        {
            string body;
            if (email.Room.GetAllInhabitants().Any())
            {
                body =$@"Покинули комнату:{email.Changed.GetPlayerList()}

Остались в комнате:{email.Room.GetAllInhabitants().GetPlayerList()}";
            }
            else
            {
                body =
                    $"Все жители покинули комнату:{email.Changed.GetPlayerList()}";
            }
            await SendRoomEmail(email, body);
        }

        public async Task Email(LeaveRoomEmail email)
        {
            string body = $"{email.Claim?.Player?.GetDisplayName()} покинул комнату, так как его заявка была отзована или отклонена.";
            if (email.Room.GetAllInhabitants().Any())
            {
                body += $"\n\nОстались в комнате:{email.Room.GetAllInhabitants().GetPlayerList()}";
            }
            await SendRoomEmail(email, body);
        }

        public async Task Email(OccupyRoomEmail email)
        {
            var oldInhabitants = email.Room.GetAllInhabitants().Except(email.Changed).ToList();
            var body = $"Вселились в комнату:{email.Changed.GetPlayerList()}";
            if (oldInhabitants.Any())
            {
                body += $"\n\nУже были в комнате:{oldInhabitants.GetPlayerList()}";
            }

            
            await SendRoomEmail(email, body);
        }

        private async Task SendRoomEmail(RoomEmailBase email, string body)
        {
            await MessageService.SendEmail(email, $"{email.ProjectName}: комната {email.Room.ProjectAccommodationType.Name} {email.Room.Name}",
                $@"{StandartGreeting()}
Изменен состав жителей комнаты {email.Room.ProjectAccommodationType.Name} {email.Room.Name} 

{body}

{email.Initiator.GetDisplayName()}

");
        }

        public Task Email(NewInviteEmail email)
        {
            string body = $"{email.Initiator.GetDisplayName()} отправил Вам приглашение к совместному проживанию.";
    
            return SendInviteEmail(email, body);
        }

        public Task Email(DeclineInviteEmail email)
        {
            string body = $"{email.Initiator.GetDisplayName()} отменил приглашение к совместному проживанию.";

            return SendInviteEmail(email, body);
        }

        public Task Email(AcceptInviteEmail email)
        {
            string body = $"{email.Initiator.GetDisplayName()} принял Ваше приглашение к совместному проживанию.";

            return SendInviteEmail(email, body);
        }

        private async Task SendInviteEmail(InviteEmailModel email, string body)
        {

            var messageTemplate = $@"{StandartGreeting()}

{body}

Вы можете управлять приглашениями на странице Вашей заявки {{0}}

{email.Initiator.GetDisplayName()}

";

            var sendTasks = email.Recipients.Select(emailRecipient => MessageService.SendEmail($"{email.ProjectName}: приглашения к проживанию",
                    new MarkdownString(String.Format(messageTemplate, email.GetClaimByPerson(emailRecipient)==null? "" :_uriService.Get(email.GetClaimByPerson(emailRecipient)))),
                    email.Initiator.ToRecepientData(),
                    emailRecipient.ToRecepientData()))
                .ToList();

            await Task.WhenAll(sendTasks).ConfigureAwait(false);
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
            if (model.Text.Contents == null)
            {
                throw new ArgumentNullException(nameof(model.Text.Contents));
            }

            var body = Regex.Replace(model.Text.Contents, EmailTokens.Name, MessageService.GetRecepientPlaceholderName(),
              RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            await MessageService.SendEmail(model, $"{model.ProjectName}: {model.Subject}",
                $@"{body}

{model.Initiator.GetDisplayName()}
");
        }
        #endregion

        /// <summary>
        /// Gets info about changed fields and other attributes for particular user (if available).
        /// </summary>
        private string GetChangedFieldsInfoForUser(
          [NotNull]EmailModelBase model,
          [NotNull]User user)
        {
            if (!(model is IEmailWithUpdatedFieldsInfo mailWithFields))
            {
                return "";
            }
            //Add project fields that user has right to view
            var accessArguments = mailWithFields.FieldsContainer.GetAccessArguments(user.UserId);

            IEnumerable<MarkdownString> fieldString = mailWithFields
              .UpdatedFields
              .Where(f => f.HasViewAccess(accessArguments))
              .Select(updatedField =>
                new MarkdownString(
                  $@"__**{updatedField.Field.FieldName}:**__
{MarkdownTransformations.HighlightDiffPlaceholder(updatedField.DisplayString, updatedField.PreviousDisplayString).Contents}"));

            //Add info about other changed atttributes (no access rights validation)
            IEnumerable<MarkdownString> otherAttributesStrings = mailWithFields
              .OtherChangedAttributes
              .Select(changedAttribute => new MarkdownString(
                $@"__**{changedAttribute.Key}:**__
{MarkdownTransformations.HighlightDiffPlaceholder(changedAttribute.Value.DisplayString, changedAttribute.Value.PreviousDisplayString).Contents}"));

            return string.Join(
              "\n\n",
              otherAttributesStrings
                .Union(fieldString)
                .Select(x => x.ToHtmlString()));
        }

        private async Task SendClaimEmail([NotNull] ClaimEmailModel model, string actionName = null, string text = "")
        {
            var projectEmailEnabled = model.GetEmailEnabled();
            if (!projectEmailEnabled)
            {
                return;
            }

            var recipients = model
              .GetRecipients()
              .Select(r => new RecepientData(
                r.GetDisplayName(),
                r.Email,
                new Dictionary<string, string> { { ChangedFieldsKey, GetChangedFieldsInfoForUser(model, r) } }))
              .ToList();

            var commentExtraActionView = (CommonUI.Models.CommentExtraAction?)model.CommentExtraAction;

            var extraText = commentExtraActionView?.GetDisplayName();

            actionName = actionName ?? commentExtraActionView?.GetShortNameOrDefault() ?? "изменена";

            if (extraText != null)
            {
                extraText = "**" + extraText + "**\n\n";
            }

            string text1 = $@"{StandartGreeting()}
Заявка {model.Claim.Name} игрока {model.Claim.Player.GetDisplayName()} {actionName} {model.GetInitiatorString()}
{text}

{MessageService.GetUserDependentValue(ChangedFieldsKey)}
{extraText}{model.Text.Contents}

{model.Initiator.GetDisplayName()}

Чтобы ответить на комментарий, перейдите на страницу заявки: {_uriService.Get(model.Claim.CommentDiscussion)}
";

            await MessageService.SendEmails(model.GetClaimEmailTitle(),
                new MarkdownString(text1),
                model.Initiator.ToRecepientData(),
                recipients);
        }

        private const string ClaimUriKey = "claimUri";

        public async Task Email([NotNull] PublishPlotElementEmail email)
        {                
            string plotElementId = $@"#pe{email.PlotElement.PlotElementId}";

            string subject = $@"{email.ProjectName}: опубликована вводная";
            string body = $@"{StandartGreeting()}"
                + $"\nПрочитать вводную: [Link]({MessageService.GetUserDependentValue(ClaimUriKey)})"
                + $"\n\n{email.Text.Contents}";

            List<RecepientData> recipients = email.Claims
                .Select(c => c.Player.ToRecepientData(new Dictionary<string, string> {
                    { ClaimUriKey, _uriService.Get(c) + plotElementId } }))
                .ToList();

            await MessageService.SendEmails(subject, new MarkdownString(body),
                email.Initiator.ToRecepientData(), recipients);
        }
    }
}
