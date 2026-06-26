using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record ProjectFieldVariantIdentification(ProjectFieldIdentification FieldId, int ProjectFieldVariantId) : IProjectEntityId
{
}
