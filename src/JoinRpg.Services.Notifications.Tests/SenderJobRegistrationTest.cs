using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Notifications.Senders;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Notifications.Tests;

public class SenderJobRegistrationTest
{
    [Fact]
    public void AddSenderJob_RegistersISenderJob_AsSameInstance()
    {
        var services = new ServiceCollection();
        services.AddSenderJob<FakeSenderJob>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var concrete = scope.ServiceProvider.GetRequiredService<FakeSenderJob>();
        var viaInterface = scope.ServiceProvider.GetRequiredService<ISenderJob>();

        viaInterface.ShouldBeSameAs(concrete);
    }

    [Fact]
    public void AddSenderJob_ExposesChannel_ViaInstanceChannel()
    {
        var services = new ServiceCollection();
        services.AddSenderJob<FakeSenderJob>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var senderJobs = scope.ServiceProvider.GetServices<ISenderJob>().ToArray();

        senderJobs.Length.ShouldBe(1);
        senderJobs[0].InstanceChannel.ShouldBe(NotificationChannel.Email);
    }

    private sealed class FakeSenderJob : ISenderJob
    {
        public static NotificationChannel Channel => NotificationChannel.Email;

        public NotificationChannel InstanceChannel => Channel;

        public bool Enabled => true;

        public Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
            => Task.FromResult(SendingResult.Success());
    }
}
