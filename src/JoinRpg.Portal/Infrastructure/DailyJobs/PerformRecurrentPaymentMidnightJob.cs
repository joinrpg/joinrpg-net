using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public class PerformRecurrentPaymentMidnightJob(IPaymentsService paymentsService, ILogger<PerformRecurrentPaymentMidnightJob> logger) : IDailyJob
{
    async Task IDailyJob.RunOnce(CancellationToken cancellationToken)
    {
        logger.LogError("NOT IMPLEMENTED");
        // paymentService.PerformAllReccurentPayments
        await Task.Delay(TimeSpan.FromSeconds(5));
        // paymentService.UpdateClaimPaymentAsync()
    }
}
