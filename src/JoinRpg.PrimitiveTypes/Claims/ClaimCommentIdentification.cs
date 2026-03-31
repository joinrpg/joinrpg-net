using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Claims;

[method: JsonConstructor]
[ProjectEntityId(AdditionalPrefixes = ["ClaimComment"])]
public partial record class ClaimCommentIdentification(ClaimIdentification ClaimId, int CommentId)
{
}
