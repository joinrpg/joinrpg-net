using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

[method: JsonConstructor]
[TypedEntityId]
public partial record PaymentTypeIdentification(
    ProjectIdentification ProjectId,
    int PaymentTypeId)
{
}
