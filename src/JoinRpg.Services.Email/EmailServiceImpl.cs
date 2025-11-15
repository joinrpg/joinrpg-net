using System.Text.RegularExpressions;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces.Email;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Email;

/// <summary>
/// Service that send all email notifications
/// </summary>
internal partial class EmailServiceImpl(IUriService uriService, IEmailSendingService messageService) : IEmailService
{
    #region general stuff
    private const string ChangedFieldsKey = "changedFields";

    private string StandartGreeting() => $"Добрый день, {messageService.GetRecepientPlaceholderName()},\n";
    #endregion

    #region IEmailService implementation
    public Task Email(AddCommentEmail model) => SendClaimEmail(model, "откомментирована");

    public Task Email(ApproveByMasterEmail model) => SendClaimEmail(model);

    public Task Email(DeclineByMasterEmail model) => SendClaimEmail(model, "отклонена");

    public Task Email(DeclineByPlayerEmail model) => SendClaimEmail(model, "отозвана");

    public Task Email(NewClaimEmail model) => SendClaimEmail(model, "подана");

    public Task Email(RestoreByMasterEmail model) => SendClaimEmail(model, "восстановлена");

    public Task Email(MoveByMasterEmail model) => SendClaimEmail(model, "изменена", $@"Заявка перенесена {model.GetInitiatorString()} на новую роль «{model.Claim.Character.CharacterName}».");


    public Task Email(ChangeResponsibleMasterEmail model) => SendClaimEmail(model, "изменена", "В заявке изменен ответственный мастер.");

    public Task Email(OnHoldByMasterEmail createClaimEmail) => SendClaimEmail(createClaimEmail, "изменена", "Заявка поставлена в лист ожидания");

    public Task Email(CheckedInEmal createClaimEmail) => SendClaimEmail(createClaimEmail, "изменена", "Игрок прошел регистрацию на полигоне");

    public Task Email(SecondRoleEmail createClaimEmail) => SendClaimEmail(createClaimEmail, "изменена", "Игрок выпущен новой ролью");

    public async Task Email(ForumEmail model)
    {
        await messageService.SendEmail(model, $"{model.ProjectName}: тема на форуме {model.ForumThread.Header}",
            StandartGreeting() + $@"
На форуме появилось новое сообщение: 

{model.Text.Contents}

{model.Initiator.GetDisplayName()}

Чтобы ответить на комментарий, перейдите на страницу обсуждения: {uriService.Get(model.ForumThread.CommentDiscussion)}
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
          .Select(r => r.ToRecepientData(
            new Dictionary<string, string> { { ChangedFieldsKey, GetChangedFieldsInfoForUser(model, r) } }))
          .Where(r => !string.IsNullOrEmpty(r.RecipientSpecificValues[ChangedFieldsKey]))
          //don't email if no changes are visible to user rights
          .ToList();

        string Target(bool forMessageBody) => model.IsCharacterMail
            ? $@"персонаж{(forMessageBody ? "a" : "")}  {model.Name}"
            : $"заявк{(forMessageBody ? "и" : "a")} {model.Name} {(forMessageBody ? $", игрок {model.Claim?.Player.GetDisplayName()}" : "")}";


        var linkString = uriService.Get(model.Linkable);

        if (recipients.Count != 0)
        {
            var text = $@"{StandartGreeting()},
Данные {Target(true)} были изменены. Новые значения:

{messageService.GetUserDependentValue(ChangedFieldsKey)}

Для просмотра всех данных перейдите на страницу {(model.IsCharacterMail ? "персонажа" : "заявки")}: {linkString}

{model.Initiator.GetDisplayName()}

";
            //All emails related to claim should have the same title, even if the change was made to a character
            var claim = model.Claim;

            var subject = claim != null
                ? model.GetClaimEmailTitle(claim)
                : $"{model.ProjectName}: {Target(false)}";

            await messageService.SendEmails(subject,
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
            body = $@"Покинули комнату:{email.Changed.GetPlayerList()}

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
        var body = $"{email.Claim?.Player?.GetDisplayName()} покинул комнату, так как его заявка была отозвана или отклонена.";
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
        if (oldInhabitants.Count != 0)
        {
            body += $"\n\nУже были в комнате:{oldInhabitants.GetPlayerList()}";
        }


        await SendRoomEmail(email, body);
    }

    private async Task SendRoomEmail(RoomEmailBase email, string body)
    {
        await messageService.SendEmail(email, $"{email.ProjectName}: комната {email.Room.ProjectAccommodationType.Name} {email.Room.Name}",
            $@"{StandartGreeting()}
Изменен состав жителей комнаты {email.Room.ProjectAccommodationType.Name} {email.Room.Name} 

{body}

{email.Initiator.GetDisplayName()}

");
    }

    public Task Email(NewInviteEmail email)
    {
        var body = $"{email.Initiator.GetDisplayName()} отправил Вам приглашение к совместному проживанию.";

        return SendInviteEmail(email, body);
    }

    public Task Email(DeclineInviteEmail email)
    {
        var body = $"{email.Initiator.GetDisplayName()} отменил приглашение к совместному проживанию.";

        return SendInviteEmail(email, body);
    }

    public Task Email(AcceptInviteEmail email)
    {
        var body = $"{email.Initiator.GetDisplayName()} принял Ваше приглашение к совместному проживанию.";

        return SendInviteEmail(email, body);
    }

    private async Task SendInviteEmail(InviteEmailModel email, string body)
    {

        var messageTemplate = $@"{StandartGreeting()}

{body}

Вы можете управлять приглашениями на странице Вашей заявки {{0}}

{email.Initiator.GetDisplayName()}

";

        var sendTasks = email.Recipients.Select(emailRecipient =>
        {
            Claim? claim = email.GetClaimByPerson(emailRecipient);
            return messageService.SendEmail($"{email.ProjectName}: приглашения к проживанию",
                                new MarkdownString(string.Format(messageTemplate, claim == null ? "" : uriService.Get(claim))),
                                email.Initiator.ToRecepientData(),
                                emailRecipient.ToRecepientData());
        })
            .ToList();

        await Task.WhenAll(sendTasks).ConfigureAwait(false);
    }

    public Task Email(FinanceOperationEmail model)
    {
        var message = "";

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
        ArgumentNullException.ThrowIfNull(model.Text.Contents);

        var body = NamePlaceholderRegex().Replace(model.Text.Contents, messageService.GetRecepientPlaceholderName());

        await messageService.SendEmail(model, $"{model.ProjectName}: {model.Subject}",
            $@"{body}

{model.Initiator.GetDisplayName()}
");
    }
    #endregion

    /// <summary>
    /// Gets info about changed fields and other attributes for particular user (if available).
    /// </summary>
    private string GetChangedFieldsInfoForUser(
      EmailModelBase model,
      User user)
    {
        if (model is not IEmailWithUpdatedFieldsInfo mailWithFields)
        {
            return "";
        }
        //Add project fields that user has right to view
        var accessArguments = mailWithFields.FieldsContainer.GetAccessArguments(user.UserId);

        IEnumerable<MarkdownString> fieldString = mailWithFields
          .UpdatedFields
          .Where(f => f.Field.HasViewAccess(accessArguments))
          .Select(updatedField =>
            new MarkdownString(
              $@"__**{updatedField.Field.Name}:**__
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

    private async Task SendClaimEmail(ClaimEmailModel model, string? actionName = null, string text = "")
    {
        var projectEmailEnabled = model.GetEmailEnabled();
        if (!projectEmailEnabled)
        {
            return;
        }

        var recipients = model
          .GetRecipients()
          .Select(r => r.ToRecepientData(new Dictionary<string, string> { { ChangedFieldsKey, GetChangedFieldsInfoForUser(model, r) } }))
          .ToList();

        var commentExtraActionView = (CommonUI.Models.CommentExtraAction?)model.CommentExtraAction;

        var extraText = commentExtraActionView?.GetDisplayName();

        actionName ??= commentExtraActionView?.GetShortName() ?? "изменена";

        if (extraText != null)
        {
            extraText = "**" + extraText + "**\n\n";
        }

        var text1 = $@"{StandartGreeting()}
Заявка {model.Claim.Character.CharacterName} игрока {model.Claim.Player.GetDisplayName()} {actionName} {model.GetInitiatorString()}
{text}

{messageService.GetUserDependentValue(ChangedFieldsKey)}
{extraText}{model.Text.Contents}

{model.Initiator.GetDisplayName()}

Чтобы ответить на комментарий, перейдите на страницу заявки: {uriService.Get(model.Claim.CommentDiscussion)}
";

        await messageService.SendEmails(model.GetClaimEmailTitle(),
            new MarkdownString(text1),
            model.Initiator.ToRecepientData(),
            recipients);
    }

    private const string ClaimUriKey = "claimUri";

    public async Task Email(PublishPlotElementEmail email)
    {
        var plotElementId = $@"#pe{email.PlotElement.PlotElementId}";

        var subject = $@"{email.ProjectName}: опубликована вводная";
        var body = new MarkdownString($@"{StandartGreeting()}

Для вас опубликована вводная. Прочитать ее: {messageService.GetUserDependentValue(ClaimUriKey)}

{email.Text.Contents}");

        var recipients = email.Claims
            .DistinctBy(x => x.PlayerUserId)
            .Select(c => c.Player.ToRecepientData(new Dictionary<string, string> {
                { ClaimUriKey, uriService.Get(c) + plotElementId } }))
            .ToList();

        await messageService.SendEmails(subject, body, email.Initiator.ToRecepientData(), recipients);
    }

    [GeneratedRegex("%NAME%", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NamePlaceholderRegex();
}
