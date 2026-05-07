using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record ClaimIdentification(
    ProjectIdentification ProjectId,
    int ClaimId) : IProjectEntityId, ILinkable
{
    public LinkType LinkType => LinkType.Claim;

    public string? Identification => ClaimId.ToString();

    int? ILinkable.ProjectId => ProjectId;
}
