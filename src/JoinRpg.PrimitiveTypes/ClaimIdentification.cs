using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[ProjectEntityId]
public partial record ClaimIdentification(
    ProjectIdentification ProjectId,
    int ClaimId) : ILinkable
{
    public LinkType LinkType => LinkType.Claim;

    public string? Identification => ClaimId.ToString();

    int? ILinkable.ProjectId => ProjectId;
}
