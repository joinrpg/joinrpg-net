using System.Diagnostics;
using JoinRpg.Data.Write.Interfaces.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications.Senders;

internal class SenderJobService<TSender>(IServiceProvider serviceProvider,
    ILogger<SenderJobService<TSender>> logger,
    IOptions<NotificationWorkerOptions> workerOptions,
    IHostApplicationLifetime hostApplicationLifetime
    ) : BackgroundService
        where TSender : class, ISenderJob
{
    private static readonly string JobName = typeof(TSender).FullName!;

    private readonly NotificationWorkerOptions WorkerOptions = workerOptions.Value;

    /// <summary>
    /// Counts the subsequent cooldowns.
    /// </summary>
    private int CooldownCounter { get; set; }

    private int FailureCounter { get; set; }

    /// <summary>
    /// Counts the subsequent successes.
    /// </summary>
    private int SuccessCounter { get; set; }

    /// <summary>
    /// Identifies is queue empty.
    /// </summary>
    private bool QueueIsEmpty { get; set; }

    // Вызываем статическое свойство интерфейса. Волшебство! Но в процессе JIT компиляции превратится в константу.
    private static NotificationChannel GetChannel<TInnerSender>() where TInnerSender : ISenderJob => TInnerSender.Channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await hostApplicationLifetime.WaitForAppStartup(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Запущена итерация обработки... ");

            if (FailureCounter >= WorkerOptions.MaxSubsequentFailures)
            {
                CooldownCounter++;
                FailureCounter = 0;
                QueueIsEmpty = false;

                if (CooldownCounter <= WorkerOptions.MaxSubsequentCooldowns)
                {
                    var cooldownPause = GetCooldownDelay(CooldownCounter);
                    logger.LogWarning("The maximum number of subsequent failures has been reached. Entering cooldown for {notificationsWorkerCooldown}", cooldownPause);
                    await Task.Delay(cooldownPause, stoppingToken);
                }
                else
                {
                    logger.LogCritical("The maximum number of subsequent cooldowns has been reached. Shutting down the worker");
                    break;
                }
            }

            if (QueueIsEmpty)
            {
                logger.LogDebug("Queue is empty, pausing before next reading.");
                await Task.Delay(WorkerOptions.EmptyPause, stoppingToken);
            }

            try
            {
                await RunIteration(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при запуске итерации SenderJobService<{senderJobName}>", JobName);
                SuccessCounter = 0;
                FailureCounter++;
            }
        }
    }

    private async Task RunIteration(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        using var activity = SenderServiceActivityHolder.ActivitySource.StartActivity($"Run of {JobName}");
        activity?.AddTag("jobName", JobName);

        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        var channel = GetChannel<TSender>();
        var nextMessage = await notificationRepository.SelectNextNotificationForSending(channel);

        if (nextMessage is null)
        {
            QueueIsEmpty = true;
            return;
        }

        do
        {

            QueueIsEmpty = false;

            SendingResult sendingResult;

            try
            {
                var sender = scope.ServiceProvider.GetRequiredService<TSender>();
                stoppingToken.ThrowIfCancellationRequested();

                if (!sender.Enabled)
                {
                    logger.LogInformation("Отправка SenderJobService<{senderJobName}> отключена", JobName);
                    throw new OperationCanceledException("Выключаем");
                }

                sendingResult = await sender.SendAsync(nextMessage, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Падение при отправке сообщения {messageId}", nextMessage.MessageId);
                sendingResult = new SendingResult(Succeeded: false, Repeatable: true);
            }

            if (sendingResult.Succeeded)
            {
                SuccessCounter++;
                logger.LogInformation("Сообщение {messageId} отправлено", nextMessage.MessageId);

                if (SuccessCounter >= WorkerOptions.MinSubsequentSuccessesToStopFailureCounting)
                {
                    FailureCounter = 0;
                }

                if (SuccessCounter >= WorkerOptions.MinSubsequentSuccessesToStopCooldownCounting)
                {
                    CooldownCounter = 0;
                }

                await notificationRepository.MarkSendingSucceeded(nextMessage.MessageId, channel);
            }
            else
            {
                SuccessCounter = 0;
                FailureCounter++;

                logger.LogWarning("Сообщение {messageId} не получилось отправить", nextMessage.MessageId);

                if (nextMessage.Attempts <= WorkerOptions.MaxAttempts && sendingResult.Repeatable)
                {
                    var momentOfNextAttempt = DateTimeOffset.UtcNow.Add(GetNextAttemptDelay(nextMessage));
                    await notificationRepository.MarkEnqueued(nextMessage.MessageId, channel, momentOfNextAttempt, nextMessage.Attempts + 1);
                }
                else
                {
                    await notificationRepository.MarkSendingFailed(nextMessage.MessageId, channel);
                }

                return; // При ошибке тоже выходим, очищаем скоуп
            }

            nextMessage = await notificationRepository.SelectNextNotificationForSending(channel);
        } while (nextMessage != null);
        QueueIsEmpty = true;
    }

    private static TimeSpan GetDelay(TimeSpan basePause, int counter, double hysteresisFactor)
    {
        var targetDelay = basePause * Math.Pow(counter, 2);
        return targetDelay + (targetDelay * hysteresisFactor * (Random.Shared.NextDouble() - 0.5));
    }

    private TimeSpan GetNextAttemptDelay(TargetedNotificationMessageForRecipient message)
        => GetDelay(WorkerOptions.BaseAttemptsPause, message.Attempts, WorkerOptions.HysteresisFactor);

    private TimeSpan GetCooldownDelay(int cooldownCounter)
        => GetDelay(WorkerOptions.BaseCooldownPause, cooldownCounter, WorkerOptions.HysteresisFactor);
}

internal static class SenderServiceActivityHolder
{
    // Нужен отдельный маленький классик, так как SenderJobService — generic, и там было бы Х экземпляров
    public static readonly ActivitySource ActivitySource = new("SenderService");
}
