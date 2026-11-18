using JoinRpg.DataModel;

namespace JoinRpg.Web.Models;

public static class FinanceOperationPaymentExtensions
{
    /// <summary>
    /// Мастер, на чью кассу пришли деньги операции, — по способу оплаты из метаданных проекта, без навигации
    /// <see cref="FinanceOperation.PaymentType"/>. <c>null</c>, если операция не платёж.
    /// </summary>
    public static UserIdentification? GetPaymentMasterId(this FinanceOperation operation, ProjectInfo projectInfo)
        => operation.GetPaymentTypeId() is { } paymentTypeId
            ? projectInfo.ProjectFinanceSettings.GetRequiredPayment(paymentTypeId).User.UserId
            : null;
}
