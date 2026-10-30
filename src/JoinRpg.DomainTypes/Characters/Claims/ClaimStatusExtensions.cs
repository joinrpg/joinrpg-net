using System.Diagnostics.Contracts;

namespace JoinRpg.DomainTypes.Characters.Claims;

public static class ClaimStatusExtensions
{
    /// <summary>
    /// Заявка активна: с ней ещё идёт работа.
    /// Зеркало <c>ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active)</c> — при изменении
    /// правила править оба места (согласованность закреплена тестом).
    /// </summary>
    public static bool IsActive(this ClaimStatus claimStatus)
        => claimStatus is not (ClaimStatus.DeclinedByMaster or ClaimStatus.DeclinedByUser or ClaimStatus.OnHold);

    /// <summary>
    /// Заявка утверждена (в том числе если игрок уже зарегистрирован на игре).
    /// </summary>
    public static bool IsApproved(this ClaimStatus claimStatus)
        => claimStatus is ClaimStatus.Approved or ClaimStatus.CheckedIn;

    /// <summary>
    /// Таблица допустимых переходов статуса заявки (ADR014, §4).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Неизвестный целевой статус.</exception>
    [Pure]
    public static bool CanChangeTo(this ClaimStatus fromStatus, ClaimStatus targetStatus)
    {
        switch (targetStatus)
        {
            case ClaimStatus.Approved:
                return new[] { ClaimStatus.AddedByUser, ClaimStatus.Discussed, ClaimStatus.CheckedIn }.Contains(fromStatus);
            case ClaimStatus.OnHold:
                return
                  new[] { ClaimStatus.AddedByUser, ClaimStatus.Discussed, ClaimStatus.AddedByMaster }
                    .Contains(fromStatus);
            case ClaimStatus.AddedByUser:
                return new[]
                    {ClaimStatus.DeclinedByUser, ClaimStatus.DeclinedByMaster, ClaimStatus.OnHold}
                  .Contains(fromStatus);
            case ClaimStatus.AddedByMaster:
                return new[]
                    {ClaimStatus.DeclinedByUser, ClaimStatus.DeclinedByMaster, ClaimStatus.OnHold}
                  .Contains(fromStatus);
            case ClaimStatus.DeclinedByUser:
            case ClaimStatus.DeclinedByMaster:
                return
                  new[]
                  {
          ClaimStatus.AddedByUser, ClaimStatus.Discussed, ClaimStatus.AddedByMaster,
          ClaimStatus.Approved, ClaimStatus.OnHold,
                  }.Contains(fromStatus);
            case ClaimStatus.Discussed:
                return
                  new[] { ClaimStatus.AddedByUser, ClaimStatus.Discussed, ClaimStatus.AddedByMaster }
                    .Contains(fromStatus);
            case ClaimStatus.CheckedIn:
                return fromStatus == ClaimStatus.Approved;
            default:
                throw new ArgumentOutOfRangeException(nameof(targetStatus), targetStatus, null);
        }
    }

    /// <summary>
    /// Статус после того, как по заявке ответила противоположная сторона: из «приглашена» заявку
    /// выводит ответ игрока, из «подана» — видимый игроку ответ мастера. Остальные статусы ответ не
    /// меняет.
    /// </summary>
    /// <param name="status">Текущий статус.</param>
    /// <param name="byPlayer">Отвечает сам игрок заявки.</param>
    /// <param name="isVisibleToPlayer">Ответ виден игроку.</param>
    [Pure]
    public static ClaimStatus AfterReply(this ClaimStatus status, bool byPlayer, bool isVisibleToPlayer)
        => status switch
        {
            ClaimStatus.AddedByMaster when byPlayer => ClaimStatus.Discussed,
            ClaimStatus.AddedByUser when !byPlayer && isVisibleToPlayer => ClaimStatus.Discussed,
            _ => status,
        };
}
