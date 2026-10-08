namespace JoinRpg.DomainTypes.Characters.Claims.Finances;

public static class ClaimBalanceExtensions
{
    /// <summary>
    /// Баланс заявки поверх доменного агрегата (ADR013) — без обращения к EF.
    /// </summary>
    /// <remarks>
    /// Слагаемые те же, что в версии для EF-сущности <c>Claim</c>
    /// (<c>JoinRpg.Domain.FinanceExtensions</c>): базовый взнос (зафиксированный в заявке или
    /// взятый из расписания проекта на дату), взнос за поля, стоимость проживания.
    /// Кеша <c>Claim.FieldsFee</c> здесь нет — взнос за поля всегда считается заново.
    /// Сама формула живёт в <see cref="ClaimFinanceInfo.CalculateBalance"/>, здесь только
    /// подставляется взнос за поля: слой полей принадлежит персонажу, а не финансам заявки.
    /// Принимает заявку в составе персонажа (ADR021): метаданные проекта берутся из агрегата,
    /// а не отдельным параметром, который можно было бы передать несогласованным.
    /// </remarks>
    public static ClaimBalance CalculateBalance(this ClaimInCharacter claim, DateTime? date = null)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var fieldsFee = claim.Character.GetAllFields(claim.ClaimId).Sum(field => field.GetCurrentFee());

        return claim.Claim.Finance.CalculateBalance(fieldsFee, claim.ProjectInfo, date ?? DateTime.UtcNow);
    }
}
