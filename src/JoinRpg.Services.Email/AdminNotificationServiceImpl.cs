using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.Users;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Email;
internal class AdminNotificationServiceImpl(
    INotificationService notificationService,
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository
    )
    : IAdminNotificationService
{
    public async Task SendTestMessage()
    {
        IReadOnlyCollection<UserInfoHeader> admins = await userRepository.GetAdminUserInfoHeaders();
        var notificationEvent = new NotificationEvent(NotificationClass.AdminMessage, EntityReference: null, "Тестовое сообщение", new NotificationEventTemplate("Добрый день, %recepient.name%!\n\nЭто тестовое сообщение"),
            [.. admins.Select(a => NotificationRecepient.Admin(a))], currentUserAccessor.UserIdentification);

        await notificationService.QueueNotification(notificationEvent);
    }
}
