using System.Text.Json.Serialization;

namespace JoinRpg.Common.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId]
public partial record AvatarIdentification(int Value)
{
}
