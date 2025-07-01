using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;
[method: JsonConstructor]
public record ClaimIdentification(
    ProjectIdentification ProjectId,
    int ClaimId) : IProjectEntityId
{
    public int Id => ClaimId;

    public ClaimIdentification(int projectId, int claimId) : this(new(projectId), claimId)
    {

    }

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

    public static IEnumerable<ClaimIdentification> FromList(IEnumerable<int> list, ProjectIdentification projectId) => list.Select(g => new ClaimIdentification(projectId, g));

    public override string ToString() => $"ClaimId({ClaimId}, {ProjectId})";
}
