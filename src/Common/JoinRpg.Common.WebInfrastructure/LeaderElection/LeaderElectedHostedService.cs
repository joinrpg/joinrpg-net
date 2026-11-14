using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebInfrastructure.LeaderElection;

/// <summary>
/// Держит <typeparamref name="TService"/> на этой реплике, пока она — лидер.
/// Пока лок занят другой репликой, периодически пытается его перехватить (так выживает
/// смерть лидера). Пока лок наш, рядом с сервисом крутится probe-цикл: если соединение,
/// держащее лок, умерло (сеть, рестарт БД), лок уже потерян — сервису шлём отмену
/// и идём перезахватывать. Сбои гасим с логом и бэкоффом: фоновый сервис не должен
/// ронять хост (см. #6217) и не должен долбить БД тесным циклом.
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

                // Сервис штатно завершился или лок потерян: пауза до следующей попытки,
                // чтобы не крутить тесный цикл из новых соединений и захватов лока.
                await Task.Delay(retryDelay, timeProvider, stoppingToken);
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
        if (completed == probeTask && !probeTask.IsCanceled)
        {
            // Соединение мертво — лок уже потерян, отменяем сервис и идём перезахватывать.
            logger.LogWarning("Lost connection holding leadership for {lockKey}, cancelling {service}", lockKey, typeof(TService).Name);
            await leadershipCts.CancelAsync();
        }
        else
        {
            leadershipCts.Cancel();
        }

        // В любом случае дожидаемся завершения сервиса: он может асинхронно дочищаться
        // после получения отмены, и scope не должен dispose'иться раньше этого.
        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            // Завершился по отмене — ожидаемо.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Leader-elected service {service} failed on {lockKey}", typeof(TService).Name, lockKey);
        }

        try
        {
            await probeTask;
        }
        catch (Exception)
        {
            // Исключение probe уже залогировано («lost connection») либо вызвано
            // закрытием соединения при dispose — наблюдаем его, чтобы не шуметь.
        }

        logger.LogDebug("Stopped being leader for {lockKey}", lockKey);
    }

    private async Task ProbeLoopAsync(ILeaderLockHandle handle, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(ProbeInterval, timeProvider, cancellationToken);
            await handle.ProbeAsync();
        }
    }
}
