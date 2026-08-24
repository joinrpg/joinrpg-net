using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl;

internal class AdminNotificationServiceImpl(
    INotificationService notificationService,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IKogdaIgraRepository kogdaIgraRepository
    )
    : IAdminNotificationService
{
    public async Task SendTestMessage()
    {
        var notificationEvent = new NotificationEvent(NotificationClass.AdminMessage, EntityReference: null, "Тестовое сообщение", new NotificationEventTemplate("Добрый день, %recepient.name%!\n\nЭто тестовое сообщение"),
            [NotificationRecepient.Admin(currentUserAccessor.ToUserInfoHeader())], currentUserAccessor.UserIdentification);

        await notificationService.QueueNotification(notificationEvent);
    }

    public async Task NotifyAboutNewProjectKogdaIgraStatus(
        ProjectIdentification projectId, ProjectName projectName, KogdaIgraLinkChoiceDto choice,
        KogdaIgraIdentification? gameId, string? message)
    {
        var body = choice switch
        {
            KogdaIgraLinkChoiceDto.Linked => await DescribeLinkedGame(gameId),
            KogdaIgraLinkChoiceDto.NotOnKogdaIgra => $"Мастер сообщает, что игры нет на КогдаИгре.\n\nСообщение редакторам: {message}",
            _ => throw new ArgumentOutOfRangeException(nameof(choice)),
        };

        var admins = await userRepository.GetAdminUserInfoHeaders();
        var notificationEvent = new NotificationEvent(
            NotificationClass.AdminMessage,
            EntityReference: projectId,
            Header: $"Новый проект «{projectName.Value}» — статус КогдаИгры",
            TemplateText: new NotificationEventTemplate($"Добрый день, %recepient.name%!\n\nСоздан новый проект «{projectName.Value}».\n\n{body}"),
            Recepients: [.. admins.Select(a => NotificationRecepient.Admin(a))],
            Initiator: currentUserAccessor.UserIdentification);

        await notificationService.QueueNotification(notificationEvent);
    }

    private async Task<string> DescribeLinkedGame(KogdaIgraIdentification? gameId)
    {
        if (gameId is not KogdaIgraIdentification id)
        {
            return "Мастер указал, что игра есть на КогдаИгре.";
        }
        var games = await kogdaIgraRepository.GetDataByIds([id]);
        var gameName = games.SingleOrDefault()?.Name ?? $"#{id.Value}";
        return $"Мастер указал, что игра есть на КогдаИгре: «{gameName}».";
    }
}
