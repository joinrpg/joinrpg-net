using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

[method: JsonConstructor]
[ProjectEntityId]
public partial record PaymentTypeIdentification(
    ProjectIdentification ProjectId,
    int PaymentTypeId)
{
}
