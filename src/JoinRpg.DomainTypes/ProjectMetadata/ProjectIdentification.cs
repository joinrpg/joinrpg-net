using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId(ShortName = "Project")]
public partial record ProjectIdentification(int Value) : IProjectEntityId
{
    ProjectIdentification IProjectEntityId.ProjectId => this;

    int IProjectEntityId.Id => Value;
}
