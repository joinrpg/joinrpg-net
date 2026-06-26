namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

public record ProjectFinanceSettings(bool PreferentialFeeEnabled, IReadOnlyCollection<PaymentTypeInfo> PaymentTypes)
{
    public PaymentTypeInfo? GetCashPaymentType(UserIdentification userId)
        => PaymentTypes.SingleOrDefault(pt => pt.User.UserId == userId && pt.TypeKind == PaymentTypeKind.Cash);
    public PaymentTypeInfo GetRequiredPayment(PaymentTypeIdentification paymentTypeId) => PaymentTypes.Single(pt => pt.PaymentTypeId == paymentTypeId);

    public bool CanAcceptCash(UserIdentification userId) => GetCashPaymentType(userId)?.Enabled ?? false;
}
