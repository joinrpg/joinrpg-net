using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces.Notifications;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Письма о слиянии аккаунтов (ADR024): обоим адресам, чтобы человек узнал, куда делся второй аккаунт,
/// а владелец чужого аккаунта — что его слили по ошибке.
/// </summary>
/// <remarks>
/// Адрес письма определяется при постановке в очередь, а отправитель потом грузит только инициатора
/// (робота). Поэтому письмо поглощаемому ставится до его удаления и всё равно дойдёт.
/// </remarks>
public class UserMergeNotifier(
    INotificationService notificationService,
    IVirtualUsersService virtualUsersService,
    IUserRepository userRepository)
{
    // TODO[Localize]
    private const string Header = "Объединение аккаунтов на JoinRpg.Ru";

    public Task NotifySecondary(UserInfoHeader secondary, string secondaryEmail, string primaryEmail)
        => Send(secondary, $"""
            Добрый день, %recepient.name%!

            Администратор JoinRpg.Ru объединяет ваш аккаунт {secondaryEmail} с аккаунтом {primaryEmail}.
            Заявки, комментарии и мастерство в проектах переходят в аккаунт {primaryEmail}, а аккаунт {secondaryEmail} удаляется.

            Входить теперь нужно в аккаунт {primaryEmail}.

            Если вы не просили объединять аккаунты, напишите на support@joinrpg.ru.
            """);

    public Task NotifyPrimary(UserInfoHeader primary, string secondaryEmail, string primaryEmail)
        => Send(primary, $"""
            Добрый день, %recepient.name%!

            Администратор JoinRpg.Ru объединил с вашим аккаунтом {primaryEmail} аккаунт {secondaryEmail}.
            Заявки, комментарии и мастерство в проектах из него теперь здесь, а аккаунт {secondaryEmail} удалён.

            Если вы не просили объединять аккаунты, напишите на support@joinrpg.ru.
            """);

    /// <summary>
    /// Полный отчёт всем админам сайта: кто, кого и в кого слил и что куда переехало.
    /// </summary>
    public async Task NotifyAdmins(UserMergeReport report)
    {
        var admins = await userRepository.GetAdminUserInfoHeaders();
        await notificationService.QueueNotification(new NotificationEvent(
            NotificationClass.AdminMessage,
            EntityReference: null,
            $"Объединены аккаунты {report.SecondaryEmail} → {report.PrimaryEmail}",
            new NotificationEventTemplate(BuildAdminReport(report)),
            [.. admins.Select(a => NotificationRecepient.Admin(a))],
            report.AdminId));
    }

    public static string BuildAdminReport(UserMergeReport report)
    {
        var text = new System.Text.StringBuilder();
        _ = text.AppendLine("Добрый день, %recepient.name%!")
            .AppendLine()
            .AppendLine($"Админ {report.AdminId.Value} объединил аккаунты.")
            .AppendLine()
            .AppendLine($"Поглощён и удалён: {report.Secondary.DisplayName.DisplayName}, `{report.SecondaryEmail}`, id {report.Secondary.UserId.Value}.")
            .AppendLine($"Основной: {report.Primary.DisplayName.DisplayName}, `{report.PrimaryEmail}`, id {report.Primary.UserId.Value}.")
            .AppendLine()
            .AppendLine("Переписано строк:")
            .AppendLine();
        var moved = report.Affected.Where(x => x.Value > 0).OrderBy(x => x.Key).ToList();
        foreach (var (column, count) in moved)
        {
            _ = text.AppendLine($"- `{column}`: {count}");
        }
        if (moved.Count == 0)
        {
            _ = text.AppendLine("- ничего");
        }
        _ = text.AppendLine()
            .AppendLine("Поля профиля, взятые у поглощаемого: "
                + (report.ProfileFieldsTaken.Count > 0 ? string.Join(", ", report.ProfileFieldsTaken) : "нет") + ".")
            .AppendLine()
            .AppendLine("Удалённые привязки соцсетей поглощаемого (у основного такие уже были): "
                + (report.DroppedLoginProviders.Count > 0 ? string.Join(", ", report.DroppedLoginProviders) : "нет") + ".");
        return text.ToString();
    }

    private Task Send(UserInfoHeader recipient, string text)
        => notificationService.QueueDirectNotification(
            new NotificationEvent(
                NotificationClass.UserAccount,
                EntityReference: null,
                Header,
                new NotificationEventTemplate(text),
                [NotificationRecepient.Direct(recipient.UserId, recipient.DisplayName.DisplayName)],
                virtualUsersService.RobotUserId),
            NotificationChannel.Email);
}

/// <summary>Что сделало слияние — для отчёта админам (ADR024).</summary>
/// <param name="AdminId">Кто слил.</param>
/// <param name="Affected">Сколько строк переписано по каждой колонке <c>Таблица.Колонка</c>.</param>
public record UserMergeReport(
    UserInfoHeader Primary,
    string PrimaryEmail,
    UserInfoHeader Secondary,
    string SecondaryEmail,
    UserIdentification AdminId,
    IReadOnlyDictionary<string, int> Affected,
    IReadOnlyCollection<string> ProfileFieldsTaken,
    IReadOnlyCollection<string> DroppedLoginProviders);
