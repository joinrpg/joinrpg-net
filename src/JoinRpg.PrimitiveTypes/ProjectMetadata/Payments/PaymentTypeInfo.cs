using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

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
