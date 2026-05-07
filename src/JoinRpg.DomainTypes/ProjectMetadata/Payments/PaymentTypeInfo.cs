using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

public record PaymentTypeInfo(PaymentTypeKind TypeKind, bool Enabled, UserInfoHeader User, PaymentTypeIdentification PaymentTypeId)
{
    public void EnsureActive()
    {
        if (!Enabled)
        {
            throw new PaymentTypeInfoDeactivatedException(PaymentTypeId);
        }
    }
}
