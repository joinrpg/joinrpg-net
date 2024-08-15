using JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

public record ProjectFinanceSettings(bool PreferentialFeeEnabled, IReadOnlyCollection<PaymentTypeInfo> PaymentTypes);
