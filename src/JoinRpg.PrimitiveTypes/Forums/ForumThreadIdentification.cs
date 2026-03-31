using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
[ProjectEntityId(AdditionalPrefixes = ["ForumThread"])]
public partial record class ForumThreadIdentification(ProjectIdentification ProjectId, int ThreadId)
{
}
