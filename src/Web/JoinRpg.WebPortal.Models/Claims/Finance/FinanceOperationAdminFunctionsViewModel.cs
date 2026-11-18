using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.Web.Models;

public class FinanceOperationAdminFunctionsViewModel
{
    public bool Allowed { get; }

    public bool PreferentialFeeEnabled { get; }

    public bool PreferentialFeeUser { get; }

    /// <summary>
    /// Параметры диалога смены размера базового взноса; <c>null</c>, если мастеру нельзя
    /// управлять деньгами заявки.
    /// </summary>
    public ChangeFeeView? ChangeFee { get; }

    public FinanceOperationAdminFunctionsViewModel(ClaimFeeViewModel source)
    {
        Allowed = source.HasFeeAdminAccess;
        PreferentialFeeEnabled = source.PreferentialFeeEnabled;
        PreferentialFeeUser = source.PreferentialFeeUser;
        // Материализуем коллекцию: параметр уходит в WebAssembly-остров и не должен быть
        // ленивым LINQ-итератором (см. комментарий про PlayerPaymentMethods в ClaimFeeViewModel).
        ChangeFee = source.HasFeeAdminAccess
            ? new ChangeFeeView(
                new ClaimIdentification(new ProjectIdentification(source.ProjectId), source.ClaimId),
                source.BaseFee,
                [.. source.FeeVariants],
                source.HasFieldsWithFee)
            : null;
    }
}
