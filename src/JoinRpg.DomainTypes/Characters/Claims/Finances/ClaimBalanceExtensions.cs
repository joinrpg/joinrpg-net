using JoinRpg.DomainTypes.ProjectMetadata;

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
    /// </remarks>
    public static ClaimBalance CalculateClaimBalance(
        this CharacterInfo character,
        CharacterClaimInfo claim,
        ProjectInfo projectInfo,
        DateTime? date = null)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(claim);

        var fieldsFee = character.GetAllFields(claim.ClaimId).Sum(field => field.GetCurrentFee());

        return claim.Finance.CalculateBalance(fieldsFee, projectInfo, date ?? DateTime.UtcNow);
    }
}
