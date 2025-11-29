using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Dal.Notifications;

/// <summary>
/// Base class for any worker class sending notification messages in background.
/// </summary>
public abstract class BaseNotificationsBackgroundService : BackgroundService
{
    protected ILogger Logger { get; }
    protected IServiceProvider Services { get; }
    protected NotificationWorkerOptions WorkerOptions { get; }

    /// <summary>
    /// Counts the subsequent cooldowns.
    /// </summary>
    protected int CooldownCounter { get; private set; }

    /// <summary>
    /// Counts the subsequent failures.
    /// </summary>
    protected int FailureCounter { get; private set; }

    /// <summary>
    /// Counts the subsequent successes.
    /// </summary>
    protected int SuccessCounter { get; private set; }

    /// <summary>
    /// Identifies is queue empty.
    /// </summary>
    protected bool QueueIsEmpty { get; private set; }

    private AsyncServiceScope? _scope = null;
    protected IServiceProvider ScopedServices => _scope?.ServiceProvider ?? throw new InvalidOperationException("Scope is not acquired");

    protected INotificationRepository NotificationsRepository { get; private set; } = null!;

    protected BaseNotificationsBackgroundService(
        ILogger logger,
        IServiceProvider services,
        NotificationWorkerOptions workerOptions)
    {
        Logger = logger;
        Services = services;
        WorkerOptions = workerOptions;
    }

    /// <summary>
    /// Identifies the notification channel this worker work with.
    /// </summary>
    protected abstract NotificationChannel Channel { get; }

    /// <summary>
    /// Describes a result of a single sending operation.
    /// </summary>
    protected struct SendingResult
    {
        public bool Succeeded { get; }

        /// <summary>
        /// When <see cref="Succeeded"/> is false, identifies is this a transient error
        /// and therefore sending could be repeated sometimes later.
        /// </summary>
        public bool Repeatable { get; }

        private SendingResult(bool succeeded, bool repeatable)
        {
            Succeeded = succeeded;
            Repeatable = repeatable;
        }

        /// <summary>
        /// Creates a sending result that identifies a sending success.
        /// </summary>
        public static SendingResult Success() => new(true, false);

        /// <summary>
        /// Creates a sending result that identifies a sending failure.
        /// </summary>
        /// <param name="repeatable">Specifies that sending could be repeated in the future.</param>
        public static SendingResult Failure(bool repeatable) => new(false, repeatable);
    }

    /// <summary>
    /// Called when new services scope has been acquired.
    /// Descendants has to re-read scoped services from it.
    /// </summary>
    /// <param name="services">Service collection.</param>
    protected abstract void ScopeChanged(IServiceProvider services);

    /// <summary>
    /// Called to send a single message. Must handle all possible errors related to the sending process.
    /// </summary>
    /// <param name="message">Message to send.</param>
    /// <param name="stoppingToken">The cancellation token which provides request of a graceful shutdown.</param>
    /// <returns>An instance of <see cref="SendingResult"/> that identifies result of a sending operation.</returns>
    protected abstract ValueTask<SendingResult> SendAsync(AddressedNotificationMessageDto message, CancellationToken stoppingToken);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await InternalExecuteAsync(stoppingToken);
            Logger.LogInformation("Worker has been gracefully terminated.");
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Worker has been gracefully terminated by external request.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unhandled exception in worker. Service has to be restarted to continue sending messages.");
        }
        finally
        {
            await TryReleaseScope();
        }
    }

    private async Task InternalExecuteAsync(CancellationToken stoppingToken)
    {
        AcquireNewScope();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (FailureCounter >= WorkerOptions.MaxSubsequentFailures)
            {
                CooldownCounter++;
                FailureCounter = 0;
                QueueIsEmpty = false;

                if (CooldownCounter <= WorkerOptions.MaxSubsequentCooldowns)
                {
                    var cooldownPause = GetCooldownDelay(CooldownCounter);
                    Logger.LogWarning("The maximum number of subsequent failures has been reached. Entering cooldown for {notificationsWorkerCooldown}", cooldownPause);
                    await SmartDelay(cooldownPause, stoppingToken);
                }
                else
                {
                    Logger.LogCritical("The maximum number of subsequent cooldowns has been reached. Shutting down the worker");
                    break;
                }
            }

            if (QueueIsEmpty)
            {
                Logger.LogDebug("Queue is empty, pausing before next reading.");
                await SmartDelay(WorkerOptions.EmptyPause, stoppingToken);
            }

            var nextMessage = await NotificationsRepository.SelectNextNotificationForSending(Channel);

            if (nextMessage is null)
            {
                QueueIsEmpty = true;
                continue;
            }

            QueueIsEmpty = false;

            SendingResult sendingResult = default;

            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                sendingResult = await SendAsync(nextMessage, stoppingToken);
            }
            catch
            {
                // Consider that the fact of unhandled exception here means message was not really sent,
                // so we can return it to the queue without consuming an attempt.
                await NotificationsRepository.MarkEnqueued(nextMessage.Id, Channel, DateTimeOffset.UtcNow, nextMessage.Attempts - 1);
                throw;
            }

            if (sendingResult.Succeeded)
            {
                SuccessCounter++;

                if (SuccessCounter >= WorkerOptions.MinSubsequentSuccessesToStopFailureCounting)
                {
                    FailureCounter = 0;
                }

                if (SuccessCounter >= WorkerOptions.MinSubsequentSuccessesToStopCooldownCounting)
                {
                    CooldownCounter = 0;
                }

                await NotificationsRepository.MarkSendingSucceeded(nextMessage.Id, Channel);
            }
            else
            {
                SuccessCounter = 0;
                FailureCounter++;

                if (nextMessage.Attempts <= WorkerOptions.MaxAttempts && sendingResult.Repeatable)
                {
                    var momentOfNextAttempt = DateTimeOffset.UtcNow.Add(GetNextAttemptDelay(nextMessage));
                    await NotificationsRepository.MarkEnqueued(nextMessage.Id, Channel, momentOfNextAttempt);
                }
                else
                {
                    await NotificationsRepository.MarkSendingFailed(nextMessage.Id, Channel);
                }
            }
        }

        stoppingToken.ThrowIfCancellationRequested();
    }

    private ValueTask TryReleaseScope()
    {
        if (_scope is null)
        {
            return ValueTask.CompletedTask;
        }

        var localScope = _scope;
        _scope = null;
        return localScope.Value.DisposeAsync();
    }

    private void AcquireNewScope()
    {
        if (_scope is not null)
        {
            throw new InvalidOperationException("Creating a new scope when another existed");
        }

        _scope = Services.CreateAsyncScope();
        NotificationsRepository = ScopedServices.GetRequiredService<INotificationRepository>();
        ScopeChanged(_scope.Value.ServiceProvider);
    }

    private async Task SmartDelay(TimeSpan duration, CancellationToken cancellationToken)
    {
        await TryReleaseScope();
        await Task.Delay(duration, cancellationToken);
        AcquireNewScope();
    }

    private static TimeSpan GetDelay(TimeSpan basePause, int counter, double hysteresisFactor)
    {
        var targetDelay = basePause * Math.Pow(counter, 2);
        return targetDelay + (targetDelay * hysteresisFactor * (Random.Shared.NextDouble() - 0.5));
    }

    private TimeSpan GetNextAttemptDelay(AddressedNotificationMessageDto message)
        => GetDelay(WorkerOptions.BaseAttemptsPause, message.Attempts, WorkerOptions.HysteresisFactor);

    private TimeSpan GetCooldownDelay(int cooldownCounter)
        => GetDelay(WorkerOptions.BaseCooldownPause, cooldownCounter, WorkerOptions.HysteresisFactor);
}
