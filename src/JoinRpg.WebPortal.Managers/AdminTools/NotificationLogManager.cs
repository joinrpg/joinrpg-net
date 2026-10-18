using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Markdown;
using JoinRpg.Web.AdminTools.Notifications;

namespace JoinRpg.WebPortal.Managers.AdminTools;

internal class NotificationLogManager(INotificationRepository notificationRepository, IUserRepository userRepository) : INotificationLogClient
{
    private const int Limit = 100;

    public async Task<NotificationLogViewModel> GetLog(UserIdentification? recipient, NotificationChannel? channel)
    {
        var items = await notificationRepository.GetNotificationLog(recipient, channel, Limit);

        UserIdentification[] userIds = [
            .. items.SelectMany(i => new[] { i.Message.Recipient, i.Message.Initiator }),
            .. recipient is null ? [] : new[] { recipient },
        ];
        var users = (await userRepository.GetUserInfoHeaders([.. userIds.Distinct()])).ToDictionary(u => u.UserId);

        UserLinkViewModel ToUser(UserIdentification id)
            => users.TryGetValue(id, out var user) ? new UserLinkViewModel(user) : UserLinkViewModel.Deleted;

        return new NotificationLogViewModel(
            recipient,
            recipient is null ? null : ToUser(recipient),
            channel,
            Limit,
            [.. items.Select(i => new NotificationLogItemViewModel(
                i.Id,
                i.Message.CreatedAt,
                i.Message.Recipient,
                ToUser(i.Message.Recipient),
                ToUser(i.Message.Initiator),
                i.Message.Header,
                i.Message.Body.ToHtmlString().Value,
                [.. i.Channels.Select(c => new NotificationDeliveryViewModel(c.Channel, c.Address, c.Status, c.Attempts, c.SendAfter))]))]);
    }
}
