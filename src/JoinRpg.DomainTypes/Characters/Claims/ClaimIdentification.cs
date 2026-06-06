using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record ClaimIdentification(
    ProjectIdentification ProjectId,
    int ClaimId) : IProjectEntityId;
