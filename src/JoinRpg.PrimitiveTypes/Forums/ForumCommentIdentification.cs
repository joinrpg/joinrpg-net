using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
[ProjectEntityId(AdditionalPrefixes = ["ForumComment"])]
public partial record class ForumCommentIdentification(ForumThreadIdentification ThreadId, int CommentId)
{
}
