namespace JoinRpg.Common.WebInfrastructure;

public static class HostRuntimeStartWaiter
{
    //https://andrewlock.net/finding-the-urls-of-an-aspnetcore-app-from-a-hosted-service-in-dotnet-6/
    public static async Task WaitForAppStartup(this IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        var cancelledSource = new TaskCompletionSource();

        using var reg1 = lifetime.ApplicationStarted.Register(startedSource.SetResult);
        using var reg2 = stoppingToken.Register(cancelledSource.SetResult);

        _ = await Task.WhenAny(startedSource.Task, cancelledSource.Task);

        stoppingToken.ThrowIfCancellationRequested();
    }
}
