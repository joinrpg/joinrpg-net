using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Drp.ManualFinance;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <summary>Собирает вход планировщика из уведомлений и выборки из базы.</summary>
internal static class FinanceSourceBuilder
{
    /// <summary>
    /// Id из уведомлений, о которых спрашиваем базу: только у финансовых событий, которые рассматривает
    /// планировщик (не раньше бэкапа за вычетом <see cref="FinanceRestorePlanner.SameTimeTolerance"/>).
    /// </summary>
    public static IReadOnlyCollection<int> NotifiedCommentIds(IEnumerable<ClaimNotificationEvent> events, DateTimeOffset backupTakenAt)
        => [.. events
            .Where(e => e.Action is CommentExtraAction.PaidFee or CommentExtraAction.RefundFee
                or CommentExtraAction.ApproveFinance or CommentExtraAction.RejectFinance)
            .Where(e => e.CreatedAt >= backupTakenAt - FinanceRestorePlanner.SameTimeTolerance)
            .Select(e => e.CommentId)
            .OfType<int>()
            .Distinct()];

    public static ProjectFinancePlan BuildPlan(
        ProjectIdentification projectId,
        IReadOnlyCollection<ClaimNotificationEvent> events,
        ClaimFinanceRestoreLookup lookup,
        DateTimeOffset backupTakenAt)
    {
        var source = new FinanceRestoreSource(
            events,
            lookup.Claims,
            [.. lookup.Operations.Select(o => new ExistingFinanceOperationInfo(o.OperationId, o.ClaimId, o.Type, o.State, o.Money, o.CreatedAt))],
            [.. lookup.Comments.Select(c => new ExistingFinanceCommentInfo(c.CommentId, c.ClaimId, c.Author, c.CreatedAt, c.ExtraAction))],
            lookup.TakenCommentIds,
            [.. lookup.PaymentTypes.Select(p => new ProjectPaymentTypeInfo(p.PaymentTypeId.PaymentTypeId, p.Kind, p.Owner, p.IsActive))],
            backupTakenAt);
        return new ProjectFinancePlan(projectId, FinanceRestorePlanner.Plan(source), lookup.PaymentTypes, ChooseActingMaster(lookup.MoneyMasters));
    }

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
