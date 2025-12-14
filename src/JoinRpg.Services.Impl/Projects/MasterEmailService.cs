using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Projects;
internal class MasterEmailService(
    IUriService uriService,
    INotificationService notificationService,
    IProjectMetadataRepository projectMetadataRepository,
    IVirtualUsersService virtualUsersService
    )
{

    public async Task EmailProjectStale(ProjectStaleMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var subject = $"{metadata.ProjectName.Value}: проект будет закрыт из-за неактивности";

        var body = $@"
Проект {metadata.ProjectName.Value} был в последний раз активен {email.LastActiveDate:yyyy-MM-dd}. Если до {email.WillCloseDate:yyyy-MM-dd} активность в нем не появится, он автоматически будет закрыт.

Подробности в справке https://docs.joinrpg.ru/project/after.html#id3

Не переживайте, закрытый проект всегда можно будет посмотреть, он не пропадет. Если проект завершен или больше не нужен, вы можете закрыть его сами.

Вы всегда можете найти его по ссылке {uriService.GetUri(email.ProjectId)}";
        await SendToAllMasters(metadata, body, subject, virtualUsersService.RobotUserId);
    }

    public async Task EmailProjectClosed(ProjectClosedMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var body = $@"Проект {metadata.ProjectName.Value} был закрыт. Вы всегда можете найти его по ссылке {uriService.GetUri(email.ProjectId)}
";
        var subject = $"{metadata.ProjectName.Value}: проект закрыт";
        await SendToAllMasters(metadata, body, subject, email.Initiator);
    }

    private async Task SendToAllMasters(ProjectInfo metadata, string body, string subject, UserIdentification initiator)
    {
        var recipients = metadata.Masters.Select(master => new NotificationRecepient(master)).ToArray();
        var notification = new NotificationEvent(NotificationClass.MasterProject, metadata.ProjectId, subject,
            new NotificationEventTemplate("Добрый день, %recepient.name%!\n\n" + body), recipients, initiator);
        await notificationService.QueueNotification(notification);

    }

    public async Task EmailProjectClosedStale(ProjectClosedStaleMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var body = $@"Проект {metadata.ProjectName.Value} был закрыт, т.к. он не был активен с {email.LastActiveDate:yyyy-MM-dd}. Вы всегда можете найти его по ссылке {uriService.GetUri(email.ProjectId)}
";
        var subject = $"{metadata.ProjectName.Value}: проект закрыт";
        await SendToAllMasters(metadata, body, subject, virtualUsersService.RobotUserId);
    }
}
