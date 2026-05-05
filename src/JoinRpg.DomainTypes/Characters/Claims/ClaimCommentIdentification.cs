using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.Claims;

[method: JsonConstructor]
[TypedEntityId]
public partial record class ClaimCommentIdentification(ClaimIdentification ClaimId, int CommentId) : IProjectEntityId
{
}
