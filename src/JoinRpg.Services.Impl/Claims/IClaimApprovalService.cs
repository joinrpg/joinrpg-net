namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Узкий внутренний контракт «утвердить заявку». Нужен автоприёму (<see cref="ClaimAutoApproveService"/>),
/// чтобы не тащить в него весь <c>IClaimService</c> — и чтобы между ними не возникло циклической
/// зависимости в DI.
/// </summary>
internal interface IClaimApprovalService
{
    /// <summary>
    /// Утверждает заявку от имени текущего пользователя. Полноценная операция: собственные проверки
    /// прав, собственное время, собственное сохранение.
    /// </summary>
    Task ApproveByMaster(ClaimIdentification claimId, string commentText);
}
