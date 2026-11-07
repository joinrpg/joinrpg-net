using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.Finances;

[method: JsonConstructor]
[TypedEntityId]
public partial record ExpenseCategoryIdentification(
    ProjectIdentification ProjectId,
    int ExpenseCategoryId) : IProjectEntityId
{
}
