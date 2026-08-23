using System.Text.Json.Serialization;

namespace JoinRpg.Common.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record class UserIdentification(int Value)
{
}
