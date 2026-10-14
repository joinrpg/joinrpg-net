using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Portal.Test.Identity;

public class AccountServiceEmailImplTest
{
    [Fact]
    public async Task PasswordChanged()
    {
        var notifications = new FakeNotificationService();
        var service = new AccountServiceEmailImpl(notifications, new FakeVirtualUsersService());

        await service.PasswordChangedEmail(new JoinIdentityUser() { UserName = "player@example.com", Id = 42 });

        var letter = notifications.Queued.ShouldHaveSingleItem();
        letter.NotificationClass.ShouldBe(NotificationClass.UserAccount);
        letter.Recepients.ShouldHaveSingleItem().UserId.ShouldBe(new UserIdentification(42));
        notifications.DirectChannels.ShouldBe([NotificationChannel.Email]);
        await Verify($"{letter.Header}\n\n{letter.TemplateText.TemplateContents}");
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public List<NotificationEvent> Queued { get; } = [];

        public List<NotificationChannel> DirectChannels { get; } = [];

        public Task QueueNotification(NotificationEvent notificationMessage) => throw new NotSupportedException();

        public Task QueueDirectNotification(NotificationEvent notificationMessage, NotificationChannel directChannel)
        {
            Queued.Add(notificationMessage);
            DirectChannels.Add(directChannel);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeVirtualUsersService : IVirtualUsersService
    {
        public User PaymentsUser => throw new NotSupportedException();

        public User RobotUser => throw new NotSupportedException();

        public UserIdentification RobotUserId => new(int.MaxValue);
    }
}
