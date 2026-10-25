using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.Web.Models;

public static class FinanceModerationViewBuilder
{
    /// <summary>
    /// Сводка для диалога модерации — только если операция ждёт модерации и пользователь вправе её модерировать.
    /// Онлайн-оплата в состоянии «предложена» модерации не требует: её статус обновляет банк.
    /// </summary>
    public static FinanceModerationOperationView? ToModerationViewIfAllowed(
        this FinanceOperation operation,
        ProjectInfo projectInfo,
        UserIdentification? currentUser)
        => operation.RequireModeration
            && projectInfo.CanModerateFinanceOperation(operation.GetPaymentTypeId(), currentUser)
            ? operation.ToModerationView(projectInfo)
            : null;

    /// <summary>
    /// Сводка операции для диалога модерации. Способ оплаты — из метаданных проекта по скалярному id, без
    /// навигации <see cref="FinanceOperation.PaymentType"/>.
    /// </summary>
    public static FinanceModerationOperationView ToModerationView(this FinanceOperation operation, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(projectInfo);

        var paymentType = operation.GetPaymentTypeId() is { } paymentTypeId
            ? projectInfo.ProjectFinanceSettings.GetPaymentByIdOrDefault(paymentTypeId)
            : null;

        return new FinanceModerationOperationView(
            operation.GetId(),
            ((FinanceOperationTypeViewModel)operation.OperationType).GetDisplayName(),
            operation.MoneyAmount,
            DateOnly.FromDateTime(operation.OperationDate),
            paymentType?.GetDisplayName());
    }
}
