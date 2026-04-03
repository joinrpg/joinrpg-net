using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId(ShortName = "KogdaIgra")]
public partial record struct KogdaIgraIdentification(int Value);
