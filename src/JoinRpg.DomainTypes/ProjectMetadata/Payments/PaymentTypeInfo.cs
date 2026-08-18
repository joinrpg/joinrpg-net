using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

public record PaymentTypeInfo(PaymentTypeKind TypeKind, bool Enabled, UserInfoHeader User, PaymentTypeIdentification PaymentTypeId, string Name, bool IsDefault)
{
    public void EnsureActive()
    {
        if (!Enabled)
        {
            throw new PaymentTypeInfoDeactivatedException(PaymentTypeId);
        }
    }
}
