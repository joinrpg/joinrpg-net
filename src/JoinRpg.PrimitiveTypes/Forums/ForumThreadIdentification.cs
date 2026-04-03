using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
[TypedEntityId]
public partial record struct ForumThreadIdentification(ProjectIdentification ProjectId, int ThreadId) : IProjectEntityId
{
}
