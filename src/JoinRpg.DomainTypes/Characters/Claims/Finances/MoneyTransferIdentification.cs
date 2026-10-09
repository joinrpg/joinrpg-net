using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes;

/// <summary>
/// Идентификатор перевода денег между мастерами проекта.
/// </summary>
[method: JsonConstructor]
[TypedEntityId]
public partial record MoneyTransferIdentification(
    ProjectIdentification ProjectId,
    int MoneyTransferId) : IProjectEntityId
{
}
