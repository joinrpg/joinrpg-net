namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Требование доступа к заявке — обязательный параметр каждой операции (ADR014). Проверяется до
/// того, как управление уйдёт в лямбду, теми же правилами, что и legacy <c>Claim.RequestAccess</c>:
/// ACL проекта плюс послабления «я игрок» и «я ответственный мастер».
/// </summary>
/// <remarks>
/// <para>
/// Это закрытый набор именованных требований, а не пара «право + причина» и не делегат. Почти все
/// требования — константы, известные в месте вызова. Исключение ровно одно —
/// <see cref="AccommodationChange"/>, где послабление зависит от статуса заявки, а статус известен
/// лишь после загрузки. Разворачивает требования сам props-сервис.
/// </para>
/// <para>
/// Admin-bypass здесь отсутствует — в отличие от операций над метаданными проекта. См. ADR014.
/// </para>
/// </remarks>
internal enum ClaimAccessRequirement
{
    /// <summary>
    /// Только игрок, подавший заявку. Мастерские права не помогают.
    /// Соответствует legacy <c>LoadClaimAsPlayer</c>.
    /// </summary>
    PlayerOnly,

    /// <summary>
    /// Мастер проекта либо сам игрок. Соответствует
    /// <c>LoadClaimAsMaster(claimId, Permission.None, ExtraAccessReason.Player)</c>.
    /// </summary>
    MasterOrPlayer,

    /// <summary>
    /// Любой мастер проекта, без дополнительных послаблений.
    /// Соответствует <c>LoadClaimAsMaster(claimId)</c>.
    /// </summary>
    AnyMaster,

    /// <summary>
    /// Право управлять заявками либо ответственный мастер этой заявки. Соответствует
    /// <c>LoadClaimForApprovalDecline</c> — шести вызовам в <c>ClaimServiceImpl</c>.
    /// </summary>
    ApprovalDecline,

    /// <summary>
    /// Право управлять деньгами проекта.
    /// </summary>
    ManageMoney,

    /// <summary>
    /// Смена поселения: право <c>CanSetPlayersAccommodations</c>, а у <b>утверждённой</b> заявки —
    /// также сам игрок и ответственный мастер. Единственное требование, зависящее от данных.
    /// </summary>
    AccommodationChange,
}
