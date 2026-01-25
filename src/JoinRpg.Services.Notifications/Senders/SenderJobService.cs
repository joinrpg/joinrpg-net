using System.Diagnostics;
using System.Diagnostics.Metrics;
using JoinRpg.Data.Write.Interfaces.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications.Senders;

internal class SenderJobService<TSender> : BackgroundService
        where TSender : class, ISenderJob
{
    private static readonly string JobName = typeof(TSender).FullName!;

    private readonly NotificationWorkerOptions WorkerOptions;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<SenderJobService<TSender>> logger;
    private readonly IHostApplicationLifetime hostApplicationLifetime;

    private readonly Counter<int> numberOfIndividualFailuresCounter;
    private readonly Counter<int> mumberOfIndividualTerminalFailuresCounter;
    private readonly Counter<int> numberOfCommonFailuresCounter;
    private readonly Counter<int> numberOfSuccessCounter;

    public SenderJobService(IServiceProvider serviceProvider,
        ILogger<SenderJobService<TSender>> logger,
        IOptions<NotificationWorkerOptions> workerOptions,
        IHostApplicationLifetime hostApplicationLifetime,
        IMeterFactory meterFactory
    )
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.hostApplicationLifetime = hostApplicationLifetime;
        WorkerOptions = workerOptions.Value;
        var meter = meterFactory.Create(JobName);
        numberOfIndividualFailuresCounter = meter.CreateCounter<int>(JobName.ToLowerInvariant() + "." + "indvidual_failures");
        mumberOfIndividualTerminalFailuresCounter = meter.CreateCounter<int>(JobName.ToLowerInvariant() + "." + "indvidual_terminal_failures");
        numberOfCommonFailuresCounter = meter.CreateCounter<int>(JobName.ToLowerInvariant() + "." + "common_failures");
        numberOfSuccessCounter = meter.CreateCounter<int>(JobName.ToLowerInvariant() + "." + "success");

    }

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
                sendingResult = SendingResult.RepeatableFailure();
            }

            if (sendingResult.Succeeded)
            {

                numberOfSuccessCounter.Add(1);
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

                if (sendingResult.Common)
                {
                    numberOfCommonFailuresCounter.Add(1);
                    logger.LogError("Произошла общая ошибка отправки при отправке сообщения {messageId}", nextMessage.MessageId);
                    FailureCounter = WorkerOptions.MaxSubsequentFailures; // Сразу выходим в кулдаун, не ждем пока накопятся ошибки.
                }
                else if (nextMessage.Attempts <= WorkerOptions.MaxAttempts && sendingResult.Repeatable)
                {
                    numberOfIndividualFailuresCounter.Add(1);
                    var momentOfNextAttempt = DateTimeOffset.UtcNow.Add(GetNextAttemptDelay(nextMessage));
                    // увеличиваем общий счетчик ошибок по сообщению, если только проблема связана с ним
                    var nextAttempt = nextMessage.Attempts + 1;

                    logger.LogWarning("Сообщение {messageId} не получилось отправить, откладываем до {nextAttemptMoment}. Это будет попытка {attemptCount}", nextMessage.MessageId, momentOfNextAttempt, nextMessage.Attempts);
                    await notificationRepository.MarkEnqueued(nextMessage.MessageId, channel, momentOfNextAttempt, nextAttempt);

                }

                else
                {
                    mumberOfIndividualTerminalFailuresCounter.Add(1);
                    numberOfIndividualFailuresCounter.Add(1);
                    logger.LogError("Сообщение {messageId} не получилось отправить, это уже {attemptCount} ошибка, помечаем как неуспешное", nextMessage.MessageId, nextMessage.Attempts);
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
