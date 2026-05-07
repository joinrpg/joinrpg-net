using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

[method: JsonConstructor]
[TypedEntityId]
public partial record PaymentTypeIdentification(
    ProjectIdentification ProjectId,
    int PaymentTypeId) : IProjectEntityId
{
}
