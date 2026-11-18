using System.Text.Json.Serialization;

namespace JoinRpg.Common.PrimitiveTypes;

[method: JsonConstructor]
[TypedEntityId(ShortName = "KogdaIgra")]
public partial record KogdaIgraIdentification(int Value);
