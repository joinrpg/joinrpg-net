using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Claims;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "ClaimComment")]
public partial record class ClaimCommentIdentification(ClaimIdentification ClaimId, int CommentId)
{
}
