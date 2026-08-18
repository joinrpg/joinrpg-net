namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Базовые сведения о заявке на персонажа — часть агрегата <see cref="CharacterInfo"/> (ADR013).
/// Несёт ровно то, что нужно гридам, подсчёту проблем и расчёту баланса; полных данных о
/// комментариях (сами <c>Comment</c> / <c>CommentDiscussion</c>) и о сюжетах здесь нет.
/// </summary>
/// <param name="PlayerId">Игрок, подавший заявку.</param>
/// <param name="DenialStatus">
/// Причина отказа. Показывать её можно не всем — фильтруется снаружи по
/// <see cref="AccessArguments.CanViewDenialStatus"/>.
/// </param>
/// <param name="LastMasterCommentAt">Последний мастерский комментарий, включая невидимые игроку.</param>
/// <param name="LastVisibleMasterCommentAt">Последний мастерский комментарий, видимый игроку.</param>
/// <param name="FeePaid">Сумма подтверждённых финансовых операций по заявке.</param>
/// <param name="AccommodationFee">Стоимость выбранного проживания, 0 если проживание не выбрано.</param>
/// <param name="Fields">
/// Слой значений полей этой заявки. Хранится для каждой заявки, а не только для утверждённой:
/// без него нельзя ни показать поля заявки в обсуждении, ни посчитать её <c>ClaimFieldsFee</c>.
/// </param>
public record class CharacterClaimInfo(
    ClaimIdentification ClaimId,
    UserIdentification PlayerId,
    ClaimStatus Status,
    ClaimDenialReason? DenialStatus,
    UserIdentification ResponsibleMasterId,
    DateTime CreateDate,
    DateTime LastUpdateDateTime,
    DateTime? CheckInDate,
    DateTimeOffset? LastPlayerCommentAt,
    DateTimeOffset? LastMasterCommentAt,
    DateTimeOffset? LastVisibleMasterCommentAt,
    int? CurrentFee,
    bool PreferentialFeeUser,
    int FeePaid,
    int AccommodationFee,
    FieldLayerContainer Fields)
{
    /// <summary>Заявка утверждена (в том числе если игрок уже зарегистрирован на игре).</summary>
    public bool IsApproved => Status is ClaimStatus.Approved or ClaimStatus.CheckedIn;

    /// <summary>С заявкой ещё идёт работа.</summary>
    public bool IsActive => Status.IsActive();

    /// <summary>Заявка в обсуждении: решение по ней ещё не принято.</summary>
    public bool IsInDiscussion => Status is ClaimStatus.AddedByMaster or ClaimStatus.AddedByUser or ClaimStatus.Discussed;

    /// <summary>Заявка не отклонена — ни мастером, ни игроком.</summary>
    public bool IsPending => Status is not (ClaimStatus.DeclinedByMaster or ClaimStatus.DeclinedByUser);
}
