using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Claims;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "ClaimComment")]
public partial record class ClaimCommentIdentification(ClaimIdentification ClaimId, int CommentId)
    : ISpanParsable<ClaimCommentIdentification>, IProjectEntityId, IComparable<ClaimCommentIdentification>
{
    public ClaimCommentIdentification(int projectId, int claimId, int commentId)
        : this(new ClaimIdentification(new ProjectIdentification(projectId), claimId), commentId)
    {

    }

    public ClaimCommentIdentification(ProjectIdentification projectId, int claimId, int commentId) : this(projectId.Value, claimId, commentId)
    {

    }

    public static ClaimCommentIdentification? FromOptional(ClaimIdentification? claimId, int? commentId)
        => commentId is not null && claimId is not null ? new ClaimCommentIdentification(claimId, commentId.Value) : null;
}
