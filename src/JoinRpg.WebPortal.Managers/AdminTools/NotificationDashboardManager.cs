using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Notifications.Senders;
using JoinRpg.Web.AdminTools.Notifications;

namespace JoinRpg.WebPortal.Managers.AdminTools;

internal class NotificationDashboardManager(INotificationRepository notificationRepository, IEnumerable<ISenderJob> senderJobs) : INotificationDashboardClient
{
    public async Task<NotificationChannelStatusViewModel[]> GetStatus()
    {
        var queueLengths = await notificationRepository.GetQueueLengths();
        var jobsByChannel = senderJobs.ToLookup(j => j.InstanceChannel);

        return [.. Enum.GetValues<NotificationChannel>().Select(channel =>
        {
            var jobs = jobsByChannel[channel].ToArray();
            var error = jobs.Length switch
            {
                0 => "Не зарегистрирована ни одна джоба отправки для этого канала",
                > 1 => $"Зарегистрировано {jobs.Length} джоб отправки для этого канала вместо одной",
                _ => null,
            };
            return new NotificationChannelStatusViewModel(
                channel,
                queueLengths.GetValueOrDefault(channel),
                error is null ? jobs[0].GetType().FullName : null,
                error);
        })];
    }
}
