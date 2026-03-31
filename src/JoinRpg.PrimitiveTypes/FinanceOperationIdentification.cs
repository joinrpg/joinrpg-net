using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "FinanceOperation")]
public partial record class FinanceOperationIdentification(ProjectIdentification ProjectId, int ClaimId, int FinanceOperationId)
{
}
