using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[ProjectEntityId(AdditionalPrefixes = ["ClaimId"])]
public partial record ClaimIdentification(
    ProjectIdentification ProjectId,
    int ClaimId) : ILinkable
{
    public LinkType LinkType => LinkType.Claim;

    public string? Identification => ClaimId.ToString();

    int? ILinkable.ProjectId => ProjectId;

    public static ClaimIdentification? FromOptional(int ProjectId, int? claimId)
    {
        if (claimId is null || claimId == -1)
        {
            return null;
        }
        else
        {
            return new ClaimIdentification(ProjectId, claimId.Value);
        }
    }
}
