using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Notifications;

[method: JsonConstructor]
[TypedEntityId]
public partial record NotificationId(int Value);
