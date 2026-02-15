using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

internal class AdminNotificationServiceImpl(
    INotificationService notificationService,
    ICurrentUserAccessor currentUserAccessor
    )
    : IAdminNotificationService
{
    public async Task SendTestMessage()
    {
        var notificationEvent = new NotificationEvent(NotificationClass.AdminMessage, EntityReference: null, "Тестовое сообщение", new NotificationEventTemplate("Добрый день, %recepient.name%!\n\nЭто тестовое сообщение"),
            [NotificationRecepient.Admin(currentUserAccessor.ToUserInfoHeader())], currentUserAccessor.UserIdentification);

        await notificationService.QueueNotification(notificationEvent);
    }
}
