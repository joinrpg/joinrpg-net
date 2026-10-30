using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain;

/// <summary>
/// Запись перехода статуса в сущность заявки (ADR014, §4): допустимость берётся из
/// <see cref="ClaimStatusExtensions.CanChangeTo"/>, здесь — статус и отметки дат. Время передаётся
/// явно: единое на операцию время подставляют обёртки на контексте операции
/// (<c>ClaimStatusTransition</c> в <c>Services.Impl</c>, ADR014, §7).
/// </summary>
public static class ClaimStatusTransitionExtensions
{
    /// <exception cref="ClaimWrongStatusException">Переход из текущего статуса запрещён.</exception>
    public static void EnsureCanChangeStatus(this Claim claim, ClaimStatus targetStatus)
    {
        if (!claim.ClaimStatus.CanChangeTo(targetStatus))
        {
            throw new ClaimWrongStatusException(claim.GetId(), claim.ClaimStatus);
        }
    }

    /// <summary>
    /// Переводит заявку в <paramref name="target"/>: проверяет допустимость перехода, пишет статус,
    /// проставляет отметку даты, соответствующую целевому статусу, и обновляет
    /// <see cref="Claim.LastUpdateDateTime"/>.
    /// </summary>
    /// <exception cref="ClaimWrongStatusException">Переход из текущего статуса запрещён.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестный целевой статус.</exception>
    public static void ChangeStatus(this Claim claim, ClaimStatus target, DateTime now)
    {
        claim.EnsureCanChangeStatus(target);

        claim.ClaimStatus = target;
        StampStatusDate(claim, target, now);
        claim.LastUpdateDateTime = now;
    }

    /// <summary>
    /// То же, но без единой отметки времени. Нужен там, где перезаписывать отметку нельзя: возврат
    /// <see cref="ClaimStatus.CheckedIn"/> → <see cref="ClaimStatus.Approved"/> при выходе на вторую
    /// роль не должен затирать <see cref="Claim.MasterAcceptedDate"/> — он виден в отчётах.
    /// </summary>
    /// <exception cref="ClaimWrongStatusException">Переход из текущего статуса запрещён.</exception>
    public static void ChangeStatusKeepingTimestamps(this Claim claim, ClaimStatus target)
    {
        claim.EnsureCanChangeStatus(target);

        claim.ClaimStatus = target;
    }

    /// <summary>
    /// Восстанавливает статус, который у заявки уже был (DRP, #5481). Таблица переходов не
    /// проверяется: это не новое решение мастера, а повтор случившегося, и промежуточных шагов
    /// восстановление не знает. Отметка даты — время события <paramref name="at"/>, а
    /// <see cref="Claim.LastUpdateDateTime"/> — время восстановления <paramref name="now"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестный целевой статус.</exception>
    public static void RestoreStatus(this Claim claim, ClaimStatus target, DateTime at, DateTime now)
    {
        if (target != ClaimStatus.DeclinedByMaster)
        {
            // Причина отказа есть только у отклонённой мастером; при отказе её уведомления не знают.
            claim.ClaimDenialStatus = null;
        }
        claim.ClaimStatus = target;
        StampStatusDate(claim, target, at);
        claim.LastUpdateDateTime = now;
    }

    /// <summary>
    /// Действие по заявке выводит её из «приглашена»/«подана» в «обсуждается», если отвечает
    /// противоположная сторона (<see cref="ClaimStatusExtensions.AfterReply"/>).
    /// </summary>
    /// <remarks>
    /// <see cref="Claim.LastUpdateDateTime"/> здесь пишется повторно — то же самое делает
    /// <c>CommentHelper.SetClaimTimes</c>. Оба получают одно и то же время операции, поведение
    /// идемпотентно; ADR014 фиксирует это как известное дублирование, которое не чинится.
    /// </remarks>
    public static void MarkDiscussed(this Claim claim, int currentUserId, bool isVisibleToPlayer, DateTime now)
    {
        claim.LastUpdateDateTime = now;
        claim.ClaimStatus = claim.ClaimStatus.AfterReply(byPlayer: currentUserId == claim.PlayerUserId, isVisibleToPlayer);
    }

    /// <summary>
    /// Таблица отметок: у каждого целевого статуса ровно одна своя дата, у остальных — ни одной
    /// (так было и до миграции).
    /// </summary>
    private static void StampStatusDate(Claim claim, ClaimStatus target, DateTime now)
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
