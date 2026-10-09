using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Мастер проекта: пользователь <see cref="UserId"/> в проекте <see cref="ProjectId"/> (ADR019).
/// Нужен там, где мастер — элемент проекта наравне с остальными, например при перестановке в списке.
/// </summary>
[method: JsonConstructor]
[TypedEntityId]
public partial record ProjectMasterIdentification(
    ProjectIdentification ProjectId,
    int UserId) : IProjectEntityId
{
}
