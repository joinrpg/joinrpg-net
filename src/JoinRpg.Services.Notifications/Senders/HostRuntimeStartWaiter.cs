using Microsoft.Extensions.Hosting;

namespace JoinRpg.Services.Notifications.Senders;
// Не самое правильное место для этого, перенести
public static class HostRuntimeStartWaiter
{
    //https://andrewlock.net/finding-the-urls-of-an-aspnetcore-app-from-a-hosted-service-in-dotnet-6/
    public static async Task WaitForAppStartup(this IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        var cancelledSource = new TaskCompletionSource();

        using var reg1 = lifetime.ApplicationStarted.Register(startedSource.SetResult);
        using var reg2 = stoppingToken.Register(cancelledSource.SetResult);

        Task completedTask = await Task.WhenAny(
            startedSource.Task,
            cancelledSource.Task).ConfigureAwait(false);

        stoppingToken.ThrowIfCancellationRequested();
    }
}
