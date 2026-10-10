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

        var fieldsFee = claim.GetAllFields().Sum(field => field.GetCurrentFee());

        return claim.Claim.Finance.CalculateBalance(fieldsFee, claim.ProjectInfo, date ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Базовый взнос, который пора зафиксировать за заявкой, или <c>null</c>, если фиксировать
    /// нечего: у проекта нет расписания взносов, взнос уже зафиксирован или заявка оплачена не полностью.
    /// </summary>
    /// <remarks>
    /// Полнота оплаты сверяется с взносом на день раньше даты операции: платёж, внесённый в день
    /// подорожания, засчитывается по старой цене. Фиксируется же цена на дату операции.
    /// </remarks>
    public static int? GetFeeToFix(this ClaimInCharacter claim, DateTime operationDate)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var finance = claim.Claim.Finance;

        if (!claim.ProjectInfo.ProjectFinanceSettings.FeeSchedule.Any()
            || finance.FixedFee is not null
            || claim.CalculateBalance(operationDate.AddDays(-1)).FeeDue > 0)
        {
            return null;
        }

        return finance.GetBaseFee(claim.ProjectInfo, operationDate);
    }
}
