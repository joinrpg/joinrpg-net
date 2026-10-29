using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Characters;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Единый переход статуса заявки (ADR014, §4). Исторически статус менялся тремя способами —
/// расширением «проверить и записать», отдельной проверкой допустимости с ручным присваиванием и
/// сырым присваиванием без охраны, — а отметки дат проставлялись руками рядом с каждой записью.
/// </summary>
/// <remarks>
/// <para>
/// Сами правила живут глубже: таблица переходов и «статус после ответа» — в
/// <see cref="ClaimStatusExtensions"/> (<c>DomainTypes</c>), запись статуса и дат в сущность — в
/// <see cref="ClaimStatusTransitionExtensions"/> (<c>Domain</c>). Здесь только обёртки, которые
/// подставляют время операции и текущего пользователя: так вся операция штампует одно время
/// (ADR014, §7).
/// </para>
/// <para>
/// Заявка передаётся <b>всегда явно</b>: в <c>ApproveByMaster</c> рядом меняются статусы двух разных
/// заявок, и умолчание «текущая» там читается неоднозначно. Перегрузки «без заявки» быть не должно.
/// По той же причине методы перехода принимают базовый
/// <see cref="CharacterOperationContext"/>, а не только контекст изменения заявки: выход на вторую
/// роль меняет статус старой заявки из контекста <b>создания</b> новой.
/// </para>
/// <para>
/// Различие «писать ли отметку даты» выражено <b>именем</b> метода, а не <c>bool</c>-параметром.
/// </para>
/// </remarks>
internal static class ClaimStatusTransition
{
    /// <inheritdoc cref="ClaimStatusTransitionExtensions.ChangeStatus" path="/summary|/exception"/>
    public static void ChangeStatus(this CharacterOperationContext ctx, Claim claim, ClaimStatus target)
        => claim.ChangeStatus(target, ctx.Now);

    /// <inheritdoc cref="ClaimStatusTransitionExtensions.ChangeStatusKeepingTimestamps" path="/summary|/exception"/>
    public static void ChangeStatusKeepingTimestamps(this CharacterOperationContext ctx, Claim claim, ClaimStatus target)
        => claim.ChangeStatusKeepingTimestamps(target);

    /// <summary>
    /// Восстанавливает статус, который у заявки уже был, — по базе уведомлений после потери основной
    /// БД (DRP, #5481). Время события проверяется как время операции восстановления.
    /// </summary>
    /// <remarks>Что именно пишется — <see cref="ClaimStatusTransitionExtensions.RestoreStatus"/>.</remarks>
    public static void RestoreStatus(this ClaimMutationContext ctx, ClaimStatus target, DateTime at)
    {
        ctx.CheckOperationDate(at);

        ctx.Claim.RestoreStatus(target, at, ctx.Now);
    }

    /// <summary>
    /// Перенос <c>ClaimServiceImpl.SetDiscussed</c>: любое действие по заявке выводит её из
    /// «приглашена»/«подана» в «обсуждается», если отвечает противоположная сторона.
    /// </summary>
    /// <remarks>Правило — <see cref="ClaimStatusTransitionExtensions.MarkDiscussed"/>.</remarks>
    public static void MarkDiscussed(this ClaimMutationContext ctx, bool isVisibleToPlayer)
        => ctx.Claim.MarkDiscussed(ctx.CurrentUser.UserId, isVisibleToPlayer, ctx.Now);
}
