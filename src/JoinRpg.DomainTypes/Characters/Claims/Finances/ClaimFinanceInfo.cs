using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters.Claims.Finances;

/// <summary>
/// Финансовые факты заявки — часть <see cref="CharacterClaimInfo"/> (ADR013). Только то, что
/// лежит в БД, без расчётов: производные величины (итоговый взнос, остаток) считает
/// <see cref="CalculateBalance"/>, потому что для них нужны метаданные проекта, дата операции
/// и слой полей, которых здесь нет.
/// </summary>
/// <param name="FixedFee">
/// Базовый взнос, зафиксированный для заявки (колонка <c>Claims.CurrentFee</c>). <c>null</c> —
/// значит базовый взнос берётся из расписания проекта на дату операции.
/// </param>
/// <param name="PreferentialFeeUser">Игроку одобрен льготный взнос.</param>
/// <param name="FeePaid">Сумма подтверждённых финансовых операций по заявке.</param>
/// <param name="AccommodationFee">Стоимость выбранного проживания, 0 если проживание не выбрано.</param>
/// <param name="OperationsRequireModeration">
/// Есть ли по заявке финансовые операции, ожидающие решения мастера
/// (<c>FinanceOperation.RequireModeration</c>). Сам список операций в агрегат не входит (ADR013):
/// проблеме <c>FinanceModerationRequired</c> нужен только факт.
/// </param>
public record class ClaimFinanceInfo(
    int? FixedFee,
    bool PreferentialFeeUser,
    int FeePaid,
    int AccommodationFee,
    bool OperationsRequireModeration)
{
    /// <summary>
    /// Базовый взнос: зафиксированный в заявке либо взятый из расписания проекта на дату операции.
    /// </summary>
    public int GetBaseFee(ProjectInfo projectInfo, DateTime operationDate)
    {
        ArgumentNullException.ThrowIfNull(projectInfo);

        return FixedFee
            ?? projectInfo.ProjectFinanceSettings.GetFeeForDate(operationDate, PreferentialFeeUser);
    }

    /// <summary>
    /// Баланс заявки. Взнос за поля приходит снаружи: слой полей живёт в
    /// <see cref="CharacterInfo"/>, а не здесь — см.
    /// <see cref="ClaimBalanceExtensions.CalculateBalance(ClaimInCharacter, DateTime?)"/>.
    /// </summary>
    public ClaimBalance CalculateBalance(int fieldsFee, ProjectInfo projectInfo, DateTime operationDate)
        => new(FeePaid, GetBaseFee(projectInfo, operationDate) + fieldsFee + AccommodationFee);

    /// <summary>
    /// Базовый взнос, который пора зафиксировать за заявкой, или <c>null</c>, если фиксировать
    /// нечего: у проекта нет расписания взносов, взнос уже зафиксирован или заявка оплачена не полностью.
    /// </summary>
    /// <remarks>
    /// Полнота оплаты сверяется с взносом на день раньше даты операции: платёж, внесённый в день
    /// подорожания, засчитывается по старой цене. Фиксируется же цена на дату операции.
    /// </remarks>
    public int? GetFeeToFix(int fieldsFee, ProjectInfo projectInfo, DateTime operationDate)
    {
        ArgumentNullException.ThrowIfNull(projectInfo);

        if (!projectInfo.ProjectFinanceSettings.FeeSchedule.Any()
            || FixedFee is not null
            || CalculateBalance(fieldsFee, projectInfo, operationDate.AddDays(-1)).FeeDue > 0)
        {
            return null;
        }

        return GetBaseFee(projectInfo, operationDate);
    }
}
