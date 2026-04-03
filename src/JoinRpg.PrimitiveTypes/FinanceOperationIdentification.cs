using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record struct FinanceOperationIdentification(ProjectIdentification ProjectId, int ClaimId, int FinanceOperationId) : IProjectEntityId
{
}
