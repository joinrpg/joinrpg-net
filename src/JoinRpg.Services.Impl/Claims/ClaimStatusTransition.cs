using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Единый переход статуса заявки (ADR014, §4). До миграции статус менялся тремя способами —
/// <c>ChangeStatusWithCheck</c>, <c>EnsureCanChangeStatus</c> с ручным присваиванием и сырым
/// присваиванием без охраны, — а отметки дат проставлялись руками рядом с каждой записью.
/// </summary>
/// <remarks>
/// <para>
/// Заявка передаётся <b>всегда явно</b>: в <c>ApproveByMaster</c> рядом меняются статусы двух разных
/// заявок, и умолчание «текущая» там читается неоднозначно. Перегрузки «без заявки» быть не должно.
/// </para>
/// <para>
/// Различие «писать ли отметку даты» выражено <b>именем</b> метода, а не <c>bool</c>-параметром.
/// </para>
/// </remarks>
internal static class ClaimStatusTransition
{
    /// <summary>
    /// Переводит заявку в <paramref name="target"/>: проверяет допустимость перехода, пишет статус,
    /// проставляет отметку даты, соответствующую целевому статусу, и обновляет
    /// <see cref="Claim.LastUpdateDateTime"/>.
    /// </summary>
    /// <exception cref="ClaimWrongStatusException">Переход из текущего статуса запрещён.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестный целевой статус.</exception>
    public static void ChangeStatus(this ClaimMutationContext ctx, Claim claim, ClaimStatus target)
    {
        ctx.EnsureCanChangeStatus(claim, target);

        claim.ClaimStatus = target;
        StampDate(claim, target, ctx.Now);
        claim.LastUpdateDateTime = ctx.Now;
    }

    /// <summary>
    /// То же, но без единой отметки времени. Нужен там, где перезаписывать отметку нельзя: возврат
    /// <see cref="ClaimStatus.CheckedIn"/> → <see cref="ClaimStatus.Approved"/> при выходе на вторую
    /// роль не должен затирать <see cref="Claim.MasterAcceptedDate"/> — он виден в отчётах.
    /// </summary>
    /// <inheritdoc cref="ChangeStatus" path="/exception"/>
    public static void ChangeStatusKeepingTimestamps(this ClaimMutationContext ctx, Claim claim, ClaimStatus target)
    {
        ctx.EnsureCanChangeStatus(claim, target);

        claim.ClaimStatus = target;
    }

    /// <summary>
    /// Только проверка допустимости перехода, без записи. Нужна операциям, которые проверяют заранее,
    /// а пишут позже (регистрация — после приёма денег).
    /// </summary>
    /// <inheritdoc cref="ChangeStatus" path="/exception"/>
    public static void EnsureCanChangeStatus(this ClaimMutationContext ctx, Claim claim, ClaimStatus target)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (!claim.ClaimStatus.CanChangeTo(target))
        {
            throw new ClaimWrongStatusException(claim.GetId(), claim.ClaimStatus);
        }
    }

    /// <summary>
    /// Перенос <c>ClaimServiceImpl.SetDiscussed</c>: любое действие по заявке выводит её из
    /// «приглашена»/«подана» в «обсуждается», если отвечает противоположная сторона.
    /// </summary>
    /// <remarks>
    /// <see cref="Claim.LastUpdateDateTime"/> здесь пишется повторно — то же самое делает
    /// <c>CommentHelper.SetClaimTimes</c>. Оба получают одно и то же время операции, поведение
    /// идемпотентно; ADR014 фиксирует это как известное дублирование, которое не чинится.
    /// </remarks>
    public static void MarkDiscussed(this ClaimMutationContext ctx, bool isVisibleToPlayer)
    {
        var claim = ctx.Claim;
        var currentUserId = ctx.CurrentUser.UserId;

        claim.LastUpdateDateTime = ctx.Now;

        if (claim.ClaimStatus == ClaimStatus.AddedByMaster && currentUserId == claim.PlayerUserId)
        {
            claim.ClaimStatus = ClaimStatus.Discussed;
        }

        if (claim.ClaimStatus == ClaimStatus.AddedByUser && currentUserId != claim.PlayerUserId && isVisibleToPlayer)
        {
            claim.ClaimStatus = ClaimStatus.Discussed;
        }
    }

    /// <summary>
    /// Таблица отметок: у каждого целевого статуса ровно одна своя дата, у остальных — ни одной
    /// (так было и до миграции).
    /// </summary>
    private static void StampDate(Claim claim, ClaimStatus target, DateTime now)
    {
        switch (target)
        {
            case ClaimStatus.Approved:
                claim.MasterAcceptedDate = now;
                break;
            case ClaimStatus.DeclinedByMaster:
                claim.MasterDeclinedDate = now;
                break;
            case ClaimStatus.DeclinedByUser:
                claim.PlayerDeclinedDate = now;
                break;
            case ClaimStatus.CheckedIn:
                claim.CheckInDate = now;
                break;
            case ClaimStatus.OnHold:
            case ClaimStatus.AddedByMaster:
            case ClaimStatus.AddedByUser:
            case ClaimStatus.Discussed:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, null);
        }
    }
}
