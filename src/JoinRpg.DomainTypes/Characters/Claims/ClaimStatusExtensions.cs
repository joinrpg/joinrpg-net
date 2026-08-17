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
}
