using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.Forums;

[method: JsonConstructor]
[TypedEntityId]
public partial record class ForumCommentIdentification(ForumThreadIdentification ThreadId, int CommentId) : IProjectEntityId
{
}
