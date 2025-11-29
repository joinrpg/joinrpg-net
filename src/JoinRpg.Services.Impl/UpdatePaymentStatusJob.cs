using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Каждую ночь пытается протолкнуть зависшие операции
/// </summary>
public class UpdatePaymentStatusJob(IPaymentsService paymentsService, ILogger<UpdatePaymentStatusJob> logger, IFinanceOperationsRepository financeOperationsRepository) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var unfinishedOperations = await financeOperationsRepository.GetUnfinishedOperations();

        logger.LogInformation("Обнаружено {unfinishedOperationsCount} незавершенных операций", unfinishedOperations.Count);

        Dictionary<FinanceOperationState, int> results = [];

        foreach (var e in Enum.GetValues<FinanceOperationState>())
        {
            results.Add(e, 0);
        }

        foreach (var operationId in unfinishedOperations)
        {
            var state = await paymentsService.UpdateClaimPaymentAsync(operationId.ProjectId, operationId.ClaimId, operationId.FinanceOperationId);
            results[state]++;
        }

        foreach (var (state, count) in results)
        {
            if (count == 0)
            {
                continue;
            }

            logger.LogInformation("Итоговый результат: {count} операций перешли в состояние {financeOperationState}", count, state);
        }

        if (unfinishedOperations.Count == 1000)
        {
            logger.LogWarning("Операций было больше 1000, возможно не все обработаны сегодня");
        }
    }
}
