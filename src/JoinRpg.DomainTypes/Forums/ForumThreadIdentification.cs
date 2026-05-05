using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.Forums;

[method: JsonConstructor]
[TypedEntityId]
public partial record class ForumThreadIdentification(ProjectIdentification ProjectId, int ThreadId) : IProjectEntityId
{
}
