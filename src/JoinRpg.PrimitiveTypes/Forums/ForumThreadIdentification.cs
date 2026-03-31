using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "ForumThread", AdditionalPrefixes = ["ForumComment"])]
public partial record class ForumThreadIdentification(ProjectIdentification ProjectId, int ThreadId)
{
}
