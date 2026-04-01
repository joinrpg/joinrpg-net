using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
[TypedEntityId]
public partial record class ForumCommentIdentification(ForumThreadIdentification ThreadId, int CommentId) : IProjectEntityId
{
}
