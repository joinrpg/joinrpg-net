using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.WebPortal.Managers.Drp;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <summary>Строит план проекта по уведомлениям и выборке из базы (<see cref="FinanceRestoreInputBuilder.BuildSource"/>).</summary>
internal static class FinanceSourceBuilder
{
    public static ProjectFinancePlan BuildPlan(
        ProjectIdentification projectId,
        IReadOnlyCollection<ClaimNotificationEvent> events,
        ClaimFinanceRestoreLookup lookup,
        DateTimeOffset backupTakenAt)
        => new(
            projectId,
            FinanceRestorePlanner.Plan(FinanceRestoreInputBuilder.BuildSource(lookup, events, backupTakenAt)),
            lookup.PaymentTypes,
            ChooseActingMaster(lookup.MoneyMasters));

    /// <summary>
    /// От чьего имени применять: доменный сервис требует действующего мастера с правом на деньги, а запись
    /// «восстановлено из уведомлений» в истории заявки будет от его имени. Выбор детерминированный —
    /// владелец проекта, иначе мастер с наименьшим id, — чтобы повторный запуск шёл от того же мастера.
    /// </summary>
    public static UserIdentification? ChooseActingMaster(IReadOnlyCollection<MoneyMasterForRestore> masters)
        => masters
            .OrderByDescending(m => m.IsOwner)
            .ThenBy(m => m.UserId.Value)
            .Select(m => m.UserId)
            .FirstOrDefault();
}
