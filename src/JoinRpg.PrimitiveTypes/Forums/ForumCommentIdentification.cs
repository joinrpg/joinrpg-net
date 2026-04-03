using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
[TypedEntityId]
public partial record struct ForumCommentIdentification(ForumThreadIdentification ThreadId, int CommentId) : IProjectEntityId
{
}
