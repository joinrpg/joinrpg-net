using JoinRpg.DataModel;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

public record PaymentTypeInfo(PaymentTypeKind TypeKind, bool Enabled, int UserId);
