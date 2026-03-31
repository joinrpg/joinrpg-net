using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "FinanceOperation")]
public partial record class FinanceOperationIdentification(ProjectIdentification ProjectId, int ClaimId, int FinanceOperationId)
    : IProjectEntityId, ISpanParsable<FinanceOperationIdentification>, IComparable<FinanceOperationIdentification>
{
    public FinanceOperationIdentification(int ProjectId, int ClaimId, int FinanceOperationId) : this(new(ProjectId), ClaimId, FinanceOperationId)
    {

    }

    public static FinanceOperationIdentification? FromOptional(ProjectIdentification? ProjectId, int? ClaimId, int? FinanceOperationId)
        => ProjectId is not null && ClaimId is not null && FinanceOperationId is not null ? new FinanceOperationIdentification(ProjectId, ClaimId.Value, FinanceOperationId.Value) : null;
}
