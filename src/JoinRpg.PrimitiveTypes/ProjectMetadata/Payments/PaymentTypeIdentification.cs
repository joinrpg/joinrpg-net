using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "PaymentType")]
public partial record PaymentTypeIdentification(
    ProjectIdentification ProjectId,
    int PaymentTypeId) : IProjectEntityId, IComparable<PaymentTypeIdentification>, ISpanParsable<PaymentTypeIdentification>
{
    public PaymentTypeIdentification(int projectId, int paymentTypeId) : this(new(projectId), paymentTypeId) { }

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

    public static IEnumerable<PaymentTypeIdentification> FromList(IEnumerable<int> list, ProjectIdentification projectId) => list.Select(g => new PaymentTypeIdentification(projectId, g));
}
