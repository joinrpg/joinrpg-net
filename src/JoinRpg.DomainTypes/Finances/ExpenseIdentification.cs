using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.Finances;

[method: JsonConstructor]
[TypedEntityId]
public partial record ExpenseIdentification(
    ProjectIdentification ProjectId,
    int ExpenseId) : IProjectEntityId
{
}
