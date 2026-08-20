namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

/// <param name="FeeSchedule">
/// Расписание взносов проекта. Порядок не важен: действующая строка выбирается в <see cref="GetFeeSettingForDate"/>.
/// </param>
public record ProjectFinanceSettings(
    bool PreferentialFeeEnabled,
    IReadOnlyCollection<PaymentTypeInfo> PaymentTypes,
    IReadOnlyCollection<ProjectFeeSettingInfo> FeeSchedule)
{
    public PaymentTypeInfo? GetCashPaymentType(UserIdentification userId)
        => PaymentTypes.SingleOrDefault(pt => pt.User.UserId == userId && pt.TypeKind == PaymentTypeKind.Cash);
    public PaymentTypeInfo GetRequiredPayment(PaymentTypeIdentification paymentTypeId) => PaymentTypes.Single(pt => pt.PaymentTypeId == paymentTypeId);
    public PaymentTypeInfo? GetPaymentByIdOrDefault(PaymentTypeIdentification paymentTypeId) => PaymentTypes.SingleOrDefault(pt => pt.PaymentTypeId == paymentTypeId);

    public bool CanAcceptCash(UserIdentification userId) => GetCashPaymentType(userId)?.Enabled ?? false;

    /// <summary>
    /// Строка расписания, действующая на дату. <c>null</c>, если на эту дату взнос не назначен.
    /// </summary>
    public ProjectFeeSettingInfo? GetFeeSettingForDate(DateTime date)
        => FeeSchedule
            .Where(fee => fee.StartDate.Date <= date.Date)
            .OrderByDescending(fee => fee.StartDate.Date)
            .FirstOrDefault();

    /// <summary>
    /// Базовый взнос проекта на дату. Ноль, если взнос на эту дату не назначен, а также если
    /// запрошен льготный взнос, а он в действующей строке не задан — так же ведёт себя
    /// <c>FinanceExtensions.ProjectFeeForDate</c>.
    /// </summary>
    public int GetFeeForDate(DateTime date, bool preferential)
    {
        var feeSetting = GetFeeSettingForDate(date);
        return (preferential ? feeSetting?.PreferentialFee : feeSetting?.Fee) ?? 0;
    }
}
