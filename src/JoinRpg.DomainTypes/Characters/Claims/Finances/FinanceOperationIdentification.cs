using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record class FinanceOperationIdentification(ProjectIdentification ProjectId, int ClaimId, int FinanceOperationId) : IProjectEntityId
{
}
