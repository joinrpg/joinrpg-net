using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebInfrastructure.LeaderElection;

/// <summary>
/// Держит <typeparamref name="TService"/> на этой реплике, пока она — лидер.
/// Пока лок занят другой репликой, периодически пытается перехватить его (так выживает
/// смерть лидера). Пока лок наш, рядом с сервисом крутится probe-цикл: если выделенное
/// под лок соединение умерло (сеть, рестарт БД), лок уже потерян — сервису шлём отмену
/// и идём перезахватывать. Любые сбои гасим с логом: фоновый сервис не должен ронять хост
/// (в .NET 11 упавший BackgroundService останавливает хост целиком).
/// </summary>
internal sealed class LeaderElectedHostedService<TService>(
    ILeaderLock leaderLock,
    IServiceProvider serviceProvider,
    ILogger<LeaderElectedHostedService<TService>> logger,
    TimeProvider timeProvider,
    long lockKey,
    TimeSpan retryDelay) : BackgroundService
    where TService : class, ILeaderElectedService
{
    internal static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ILeaderLockHandle? handle = null;
            try
            {
                handle = await leaderLock.TryAcquireAsync(lockKey, stoppingToken);
                if (handle is null)
                {
                    logger.LogDebug("Not a leader for {lockKey}, will retry", lockKey);
                    await Task.Delay(retryDelay, timeProvider, stoppingToken);
                    continue;
                }

                logger.LogInformation("Became leader for {lockKey}", lockKey);
                await RunWhileLeader(handle, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Хост останавливается — выходим.
                throw;
            }
            catch (Exception ex)
            {
                // База может быть недоступна: пропускаем цикл, ретраим через retryDelay.
                logger.LogWarning(ex, "Error acquiring or holding leadership for {lockKey}, will retry", lockKey);
                await Task.Delay(retryDelay, timeProvider, stoppingToken);
            }
            finally
            {
                if (handle is not null)
                {
                    await handle.DisposeAsync();
                    logger.LogInformation("Released leadership for {lockKey}", lockKey);
                }
            }
        }
    }

    private async Task RunWhileLeader(ILeaderLockHandle handle, CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        using var leadershipCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        var runTask = service.ExecuteAsync(leadershipCts.Token);
        var probeTask = ProbeLoopAsync(handle, leadershipCts.Token);

        var completed = await Task.WhenAny(runTask, probeTask);
        if (runTask.IsFaulted)
        {
            logger.LogError(runTask.Exception!.GetBaseException(), "Leader-elected service {service} failed on {lockKey}", typeof(TService).Name, lockKey);
        }

        if (completed == probeTask && !probeTask.IsCanceled)
        {
            // Соединение мертво — лок уже потерян, отменяем сервис и идём перезахватывать.
            logger.LogWarning("Lost connection holding leadership for {lockKey}, cancelling {service}", lockKey, typeof(TService).Name);
            await leadershipCts.CancelAsync();
            try
            {
                await runTask;
            }
            catch (OperationCanceledException)
            {
                // Сервис завершился по отмене — ожидаемо.
            }
        }

        leadershipCts.Cancel();
        runTask.Exception?.Handle(_ => true);
        logger.LogDebug("Stopped being leader for {lockKey}", lockKey);
    }

    private async Task ProbeLoopAsync(ILeaderLockHandle handle, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(ProbeInterval, timeProvider, cancellationToken);
            await ((PostgresLeaderLockHandle)handle).ProbeAsync();
        }
    }
}
