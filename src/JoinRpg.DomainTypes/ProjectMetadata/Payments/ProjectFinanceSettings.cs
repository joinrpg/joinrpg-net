namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

/// <param name="FeeSchedule">
/// Расписание взносов проекта. Порядок в коллекции не важен: правильный порядок даёт
/// <see cref="FeeScheduleOrdered"/>, действующая строка выбирается в <see cref="GetFeeSettingForDate"/>.
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
    /// Расписание в порядке вступления в силу: каждая следующая строка перебивает предыдущие.
    /// При равной дате начала позже идёт созданная позже (с большим Id) — именно она и действует.
    /// Этот порядок — единственный правильный способ показывать расписание списком.
    /// </summary>
    public IReadOnlyList<ProjectFeeSettingInfo> FeeScheduleOrdered
        => [.. FeeSchedule
            .OrderBy(fee => fee.StartDate.Date)
            .ThenBy(fee => fee.ProjectFeeSettingId)];

    /// <summary>
    /// Строка расписания, действующая на дату. <c>null</c>, если на эту дату взнос не назначен.
    /// Если на одну дату заведено несколько строк, действует созданная позже (с большим Id).
    /// </summary>
    public ProjectFeeSettingInfo? GetFeeSettingForDate(DateTime date)
        => FeeScheduleOrdered
            .LastOrDefault(fee => fee.StartDate.Date <= date.Date);

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
