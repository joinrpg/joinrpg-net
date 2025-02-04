using System.Data.Common;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public class PerformRecurrentPaymentMidnightJob(IPaymentsService paymentsService, ILogger<PerformRecurrentPaymentMidnightJob> logger) : IDailyJob
{
    async Task IDailyJob.RunOnce(CancellationToken cancellationToken)
    {
        const int maxTransientErrors = 5;
        const int maxSubsequentFailedPayments = 10; // We believe that many subsequently failed payments is a sign of a bank problem (fraud protection, service down, etc)
        const int retryDelayMilliseconds = 2000;

        int? lastRecurrentPaymentId = null;
        var totalSucceededPayments = 0;
        var totalFailedPayments = 0;
        var subsequentFailedPayments = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var transientErrorsCount = 0;

            IReadOnlyList<RecurrentPayment>? recurrentPayments = null;
            do
            {
                if (transientErrorsCount > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(retryDelayMilliseconds + Random.Shared.Next(0, 500)), cancellationToken);
                }

                try
                {
                    recurrentPayments = await paymentsService.FindRecurrentPaymentsAsync(lastRecurrentPaymentId, activityStatus: true);
                    transientErrorsCount = 0;
                }
                catch (DbException ex) when (ex.IsTransient)
                {
                    // We can try to suppress transient errors for a few attempts
                    logger.LogError(ex, "A transient database error occured while reading a next pack of recurrent payments");
                    transientErrorsCount++;
                }
            } while (recurrentPayments is null && transientErrorsCount < maxTransientErrors);

            if (recurrentPayments?.Count is not > 0)
            {
                break;
            }

            if (lastRecurrentPaymentId.HasValue)
            {
                logger.LogInformation("Next {recurrentPaymentsCount} were found after recurrent payment {recurrentPaymentId}", recurrentPayments.Count, lastRecurrentPaymentId);
            }
            else
            {
                logger.LogInformation("First {recurrentPaymentsCount} were found", recurrentPayments.Count);
            }

            // Processing each recurrent payment from the loaded batch
            foreach (var rp in recurrentPayments)
            {
                if (transientErrorsCount > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(2 * retryDelayMilliseconds + Random.Shared.Next(0, 500)), cancellationToken);
                }

                try
                {
                    var fo = await paymentsService.PerformRecurrentPaymentAsync(rp, amount: null, internalCall: true);
                    transientErrorsCount = 0;

                    switch (fo?.State)
                    {
                        case FinanceOperationState.Approved:
                        case FinanceOperationState.Proposed: // Should treat as approved here — if it was not immediately declined it is a high chance it will be performed soon
                            totalSucceededPayments++;
                            subsequentFailedPayments = 0;
                            break;
                        case FinanceOperationState.Declined:
                        case FinanceOperationState.Invalid:
                            totalFailedPayments++;
                            subsequentFailedPayments++;
                            break;
                        case null:
                            totalFailedPayments++;
                            break;
                        default:
                            logger.LogError("Unexpected finance operation state {financeOperationState} while performing recurrent payment {recurrentPaymentId}", fo.State, rp.RecurrentPaymentId);
                            totalFailedPayments++;
                            break;
                    }
                }
                catch (DbException ex) when (ex.IsTransient)
                {
                    // We can try to suppress transient errors for a few subsequent attempts.
                    logger.LogError(ex, "A transient database error occured while performing recurrent payment {recurrentPaymentId}", rp.RecurrentPaymentId);
                    transientErrorsCount++;
                    totalFailedPayments++;
                    subsequentFailedPayments = 0;
                }

                if (transientErrorsCount >= maxTransientErrors)
                {
                    logger.LogError("Too many transient database errors occured — interrupting the entire processing");
                    break;
                }

                if (subsequentFailedPayments >= maxSubsequentFailedPayments)
                {
                    logger.LogError("Too many subsequently failed payments occured — interrupting the entire processing");
                    break;
                }
            }
        }

        logger.LogInformation("Done performing recurrent payments: succeeded {succeededRecurrentPayments}, failed {failedRecurrentPayments}", totalSucceededPayments, totalFailedPayments);
    }
}
