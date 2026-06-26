using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

[method: JsonConstructor]
[TypedEntityId]
public partial record PaymentTypeIdentification(
    ProjectIdentification ProjectId,
    int PaymentTypeId) : IProjectEntityId
{
}
