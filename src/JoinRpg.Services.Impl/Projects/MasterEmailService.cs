using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Projects;

internal class MasterEmailService(
    INotificationService notificationService,
    IProjectMetadataRepository projectMetadataRepository,
    IVirtualUsersService virtualUsersService,
    IUserRepository userRepository
    )
{

    public async Task EmailProjectStale(ProjectStaleMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var subject = $"{metadata.ProjectName.Value}: проект будет закрыт из-за неактивности";

        var body = $@"
Проект {metadata.ProjectName.Value} был в последний раз активен {email.LastActiveDate:yyyy-MM-dd}. Если до {email.WillCloseDate:yyyy-MM-dd} активность в нем не появится, он автоматически будет закрыт.

Подробности в справке https://docs.joinrpg.ru/project/after.html#auto-close

Не переживайте, закрытый проект всегда можно будет посмотреть, он не пропадет. Если проект завершен или больше не нужен, вы можете закрыть его сами.";
        await SendToAllMasters(metadata, body, subject, virtualUsersService.RobotUserId);
    }

    public async Task EmailProjectClosed(ProjectClosedMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var body = $@"Проект {metadata.ProjectName.Value} был закрыт.";
        var subject = $"{metadata.ProjectName.Value}: проект закрыт";
        await SendToAllMasters(metadata, body, subject, email.Initiator);
    }

    /// <summary>
    /// Снятие блокировки необратимо: восстановление больше недоступно, заблокировать снова нельзя (ADR023).
    /// Поэтому узнать должны все мастера проекта и все админы сайта.
    /// </summary>
    // TODO[Localize]
    public async Task EmailProjectUnblocked(ProjectUnblockedMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);
        var admins = await userRepository.GetAdminUserInfoHeaders();

        var body = $@"{email.Initiator.DisplayName.DisplayName} снял(а) блокировку с проекта {metadata.ProjectName.Value}.

Восстановление данных в проекте больше недоступно, заблокировать проект снова нельзя.";
        var subject = $"{metadata.ProjectName.Value}: блокировка снята";

        var masters = metadata.Masters.Select(master => new NotificationRecepient(master)).ToArray();
        // Админ, который сам мастер проекта, получает одно уведомление — как мастер.
        var masterIds = masters.Select(m => m.UserId).ToHashSet();
        var otherAdmins = admins.Where(a => !masterIds.Contains(a.UserId)).Select(a => NotificationRecepient.Admin(a));

        await Send(metadata, body, subject, email.Initiator.UserId, [.. masters, .. otherAdmins]);
    }

    private Task SendToAllMasters(ProjectInfo metadata, string body, string subject, UserIdentification initiator)
        => Send(metadata, body, subject, initiator, [.. metadata.Masters.Select(master => new NotificationRecepient(master))]);

    private async Task Send(ProjectInfo metadata, string body, string subject, UserIdentification initiator, NotificationRecepient[] recipients)
    {
        var notification = new NotificationEvent(NotificationClass.MasterProject, metadata.ProjectId, subject,
            new NotificationEventTemplate("Добрый день, %recepient.name%!\n\n" + body), recipients, initiator);
        await notificationService.QueueNotification(notification);
    }

    public async Task EmailProjectClosedStale(ProjectClosedStaleMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var body = $@"Проект {metadata.ProjectName.Value} был закрыт, т.к. он не был активен с {email.LastActiveDate:yyyy-MM-dd}.";
        var subject = $"{metadata.ProjectName.Value}: проект закрыт";
        await SendToAllMasters(metadata, body, subject, virtualUsersService.RobotUserId);
    }
}
