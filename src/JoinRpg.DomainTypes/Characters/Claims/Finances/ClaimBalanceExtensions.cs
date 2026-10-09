using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters.Claims.Finances;

public static class ClaimBalanceExtensions
{
    /// <summary>
    /// Разбивка взноса заявки поверх доменного агрегата (ADR013) — без обращения к EF.
    /// </summary>
    /// <remarks>
    /// Слагаемые те же, что в версии для EF-сущности <c>Claim</c>
    /// (<c>JoinRpg.Domain.FinanceExtensions</c>): базовый взнос (зафиксированный в заявке или
    /// взятый из расписания проекта на дату), взнос за поля, стоимость проживания.
    /// Кеша <c>Claim.FieldsFee</c> здесь нет — взнос за поля всегда считается заново.
    /// Поля берутся глазами самой заявки (<see cref="ClaimInCharacter.GetAllFields"/>), без
    /// фильтрации по правам зрителя: сколько платить, не зависит от того, кто смотрит.
    /// Принимает заявку в составе персонажа (ADR021): метаданные проекта берутся из агрегата,
    /// а не отдельным параметром, который можно было бы передать несогласованным.
    /// </remarks>
    public static ClaimFeeBreakdown CalculateFeeBreakdown(this ClaimInCharacter claim, DateTime? date = null)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var pricedFields = claim.GetAllFields().Where(field => field.Field.HasPrice).ToList();

        return new ClaimFeeBreakdown(
            claim.Claim.Finance.GetBaseFee(claim.ProjectInfo, date ?? DateTime.UtcNow),
            Subtotal(FieldBoundTo.Character),
            Subtotal(FieldBoundTo.Claim),
            claim.Claim.Finance.AccommodationFee);

        FieldsFeeSubtotal Subtotal(FieldBoundTo boundTo)
        {
            var fields = pricedFields.Where(field => field.Field.BoundTo == boundTo).ToList();
            return new FieldsFeeSubtotal(fields.Sum(field => field.GetCurrentFee()), fields.Count);
        }
    }

    /// <summary>
    /// Баланс заявки поверх доменного агрегата (ADR013) — без обращения к EF. Итоговый взнос —
    /// из <see cref="CalculateFeeBreakdown"/>.
    /// </summary>
    public static ClaimBalance CalculateBalance(this ClaimInCharacter claim, DateTime? date = null)
    {
        ArgumentNullException.ThrowIfNull(claim);

        return new ClaimBalance(claim.Claim.Finance.FeePaid, claim.CalculateFeeBreakdown(date).TotalFee);
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
