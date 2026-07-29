using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces;
using JoinRpg.Services.Notifications.Senders;
using JoinRpg.WebPortal.Managers.AdminTools;

namespace JoinRpg.WebPortal.Managers.Test.AdminTools;

public class NotificationDashboardManagerTests
{
    [Fact]
    public async Task GetStatus_ReturnsQueueLengthAndJobFullName_ForChannelWithSingleJob()
    {
        var repository = new FakeNotificationRepository(new Dictionary<NotificationChannel, int>
        {
            [NotificationChannel.Email] = 5,
        });
        var manager = new NotificationDashboardManager(repository, [new FakeSenderJob(NotificationChannel.Email)]);

        var status = await manager.GetStatus();

        var email = status.Single(s => s.Channel == NotificationChannel.Email);
        email.QueueLength.ShouldBe(5);
        email.Error.ShouldBeNull();
        email.JobFullName.ShouldBe(typeof(FakeSenderJob).FullName);
    }

    [Fact]
    public async Task GetStatus_ReturnsZeroQueueLength_WhenChannelMissingFromRepository()
    {
        var repository = new FakeNotificationRepository(new Dictionary<NotificationChannel, int>());
        var manager = new NotificationDashboardManager(repository, [new FakeSenderJob(NotificationChannel.Email)]);

        var status = await manager.GetStatus();

        status.Single(s => s.Channel == NotificationChannel.Email).QueueLength.ShouldBe(0);
    }

    [Fact]
    public async Task GetStatus_ReturnsError_WhenNoJobRegisteredForChannel()
    {
        var repository = new FakeNotificationRepository(new Dictionary<NotificationChannel, int>());
        var manager = new NotificationDashboardManager(repository, []);

        var status = await manager.GetStatus();

        var email = status.Single(s => s.Channel == NotificationChannel.Email);
        email.Error.ShouldNotBeNull();
        email.JobFullName.ShouldBeNull();
    }

    [Fact]
    public async Task GetStatus_ReturnsError_WhenMultipleJobsRegisteredForChannel()
    {
        var repository = new FakeNotificationRepository(new Dictionary<NotificationChannel, int>());
        var manager = new NotificationDashboardManager(repository,
            [new FakeSenderJob(NotificationChannel.Email), new FakeSenderJob(NotificationChannel.Email)]);

        var status = await manager.GetStatus();

        var email = status.Single(s => s.Channel == NotificationChannel.Email);
        email.Error.ShouldNotBeNull();
        email.JobFullName.ShouldBeNull();
    }

    private sealed class FakeNotificationRepository(IReadOnlyDictionary<NotificationChannel, int> queueLengths) : INotificationRepository
    {
        public Task<IReadOnlyDictionary<NotificationChannel, int>> GetQueueLengths() => Task.FromResult(queueLengths);

        public Task InsertNotifications(NotificationMessageCreateDto[] notifications) => throw new NotImplementedException();

        public Task<TargetedNotificationMessageForRecipient?> SelectNextNotificationForSending(NotificationChannel channel) => throw new NotImplementedException();

        public Task MarkSendingSucceeded(NotificationId id, NotificationChannel channel) => throw new NotImplementedException();

        public Task MarkSendingFailed(NotificationId id, NotificationChannel channel) => throw new NotImplementedException();

        public Task MarkEnqueued(NotificationId id, NotificationChannel channel, DateTimeOffset sendAfter, int? attempts = null) => throw new NotImplementedException();

        public Task<IReadOnlyCollection<NotificationHistoryDto>> GetLastNotificationsForUser(UserIdentification userId, NotificationChannel notificationChannel, KeySetPagination pagination) => throw new NotImplementedException();
    }

    private sealed class FakeSenderJob(NotificationChannel channel) : ISenderJob
    {
        public NotificationChannel InstanceChannel => channel;

        public bool Enabled => true;

        public Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
            => throw new NotImplementedException();
    }
}
