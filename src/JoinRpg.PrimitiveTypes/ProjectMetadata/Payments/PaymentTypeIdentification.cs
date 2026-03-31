using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "PaymentType")]
public partial record PaymentTypeIdentification(
    ProjectIdentification ProjectId,
    int PaymentTypeId)
{
    public static PaymentTypeIdentification? FromOptional(int ProjectId, int? paymentTypeId)
    {
        if (paymentTypeId is null || paymentTypeId == -1)
        {
            return null;
        }
        else
        {
            return new PaymentTypeIdentification(ProjectId, paymentTypeId.Value);
        }
    }
}
