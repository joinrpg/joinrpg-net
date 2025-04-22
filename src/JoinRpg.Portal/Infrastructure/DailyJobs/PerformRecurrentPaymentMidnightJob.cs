using System.Collections.Frozen;
using System.Data.Common;
using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public class PerformRecurrentPaymentMidnightJob(IPaymentsService paymentsService, ILogger<PerformRecurrentPaymentMidnightJob> logger) : IDailyJob
{
    private const int MaxTransientErrors = 5;
    private const int RetryDelayMilliseconds = 2000;
    private static readonly FrozenSet<FinanceOperationState> FinanceOperationStates = [FinanceOperationState.Proposed, FinanceOperationState.Approved];

    async Task IDailyJob.RunOnce(CancellationToken cancellationToken)
    {
        const int maxSubsequentFailedPayments = 10; // We believe that many subsequently failed payments is a sign of bank problem (fraud protection, service down, etc)

        var today = DateTime.UtcNow;

        int? lastRecurrentPaymentId = null;
        var totalSucceededPayments = 0;
        var totalFailedPayments = 0;
        var subsequentFailedPayments = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recurrentPayments = await ExecuteWithRepeatAsync(
                "ReadRecurrentPayments",
                static (param, _) => param.paymentsService.FindRecurrentPaymentsAsync(param.lastRecurrentPaymentId, activityStatus: true),
                (lastRecurrentPaymentId, paymentsService),
                cancellationToken);

            if (recurrentPayments.Count == 0)
            {
                break;
            }

            if (lastRecurrentPaymentId.HasValue)
            {
                logger.LogInformation("Next {recurrentPaymentsCount} recurrent payments were found after recurrent payment {recurrentPaymentId}", recurrentPayments.Count, lastRecurrentPaymentId);
            }
            else
            {
                logger.LogInformation("First {recurrentPaymentsCount} recurrent payments were found", recurrentPayments.Count);
            }

            lastRecurrentPaymentId = recurrentPayments[^1].RecurrentPaymentId;

            // Processing each recurrent payment from the loaded batch
            foreach (var rp in recurrentPayments)
            {
                var financeOperations = await ExecuteWithRepeatAsync(
                    $"ReadFinanceOperations({rp.RecurrentPaymentId})",
                    static (param, _) =>
                        param.paymentsService.FindOperationsOfRecurrentPaymentAsync(
                            param.rp.RecurrentPaymentId,
                            forPeriod: param.today,
                            ofStates: FinanceOperationStates,
                            pageSize: 1000),
                    (rp, today, paymentsService),
                    cancellationToken);

                var haveProposedOrApprovedPayment = financeOperations.Any(static fo => fo.State == FinanceOperationState.Approved);

                // Updating proposed payments
                foreach (var fo in financeOperations.Where(static fo => fo.State == FinanceOperationState.Proposed))
                {
                    var foState = await ExecuteWithRepeatAsync(
                        $"UpdateFinanceOperation({fo.CommentId})",
                        static (param, _) =>
                            param.paymentsService.UpdateFinanceOperationAsync(param.fo),
                        (fo, paymentsService),
                        cancellationToken);

                    if (foState is FinanceOperationState.Approved or FinanceOperationState.Proposed)
                    {
                        haveProposedOrApprovedPayment = true;
                        subsequentFailedPayments = 0;
                    }
                    else
                    {
                        subsequentFailedPayments++;
                    }
                }

                if (haveProposedOrApprovedPayment)
                {
                    continue;
                }

                if (subsequentFailedPayments >= maxSubsequentFailedPayments)
                {
                    logger.LogError("Too many subsequently failed payments occured ({subsequentFailedPaymentsCount}) — interrupting the entire processing", subsequentFailedPayments);
                    break;
                }

                // No existed payments, performing a new one
                var newFinanceOperation = await ExecuteWithRepeatAsync(
                    $"PerformRecurrentPayment({rp.RecurrentPaymentId})",
                    static (param, _) => param.paymentsService.PerformRecurrentPaymentAsync(param.rp, amount: null, internalCall: true),
                    (rp, paymentsService),
                    cancellationToken);

                switch (newFinanceOperation?.State)
                {
                    case FinanceOperationState.Approved:
                    case FinanceOperationState.Proposed: // Should treat as approved here — if it was not immediately declined it is a high chance it will be performed soon
                        totalSucceededPayments++;
                        subsequentFailedPayments = 0;
                        break;
                    case FinanceOperationState.Declined:
                    case FinanceOperationState.Invalid:
                    case FinanceOperationState.Expired:
                        totalFailedPayments++;
                        subsequentFailedPayments++;
                        break;
                    case null:
                        totalFailedPayments++;
                        break;
                    default:
                        logger.LogError(
                            "Unexpected finance operation state {financeOperationState} while performing recurrent payment {recurrentPaymentId}",
                            newFinanceOperation.State,
                            rp.RecurrentPaymentId);
                        totalFailedPayments++;
                        break;
                }
            }
        }

        logger.LogInformation("Done performing recurrent payments: succeeded {succeededRecurrentPayments}, failed {failedRecurrentPayments}", totalSucceededPayments, totalFailedPayments);
    }

    private async Task<TResult> ExecuteWithRepeatAsync<TParameter, TResult>(
        string operationName,
        Func<TParameter, CancellationToken, Task<TResult>> action,
        TParameter parameter,
        CancellationToken cancellationToken)
    {
        var transientErrorsCount = 0;

        do
        {
            if (transientErrorsCount > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(RetryDelayMilliseconds + Random.Shared.Next(0, 500)), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogDebug("Executing operation {OperationName} while processing recurrent payments, attempt {OperationAttempt}", operationName, transientErrorsCount);
                return await action(parameter, cancellationToken);
            }
            catch (DbException ex) when (ex.IsTransient)
            {
                // We can try to suppress transient errors for a few attempts
                logger.LogError(ex, "A transient database error occured while executing {OperationName}", operationName);
                transientErrorsCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured while executing {OperationName}. Recurrent payments processing will be cancelled", operationName);
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();

        } while (transientErrorsCount < MaxTransientErrors);

        throw new InvalidOperationException($"Failed to execute repeatable action {transientErrorsCount} times while performing operation {operationName}");
    }
}
