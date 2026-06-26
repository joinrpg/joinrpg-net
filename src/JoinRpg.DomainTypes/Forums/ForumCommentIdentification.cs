using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.Forums;

[method: JsonConstructor]
[TypedEntityId]
public partial record class ForumCommentIdentification(ForumThreadIdentification ThreadId, int CommentId) : IProjectEntityId
{
}
