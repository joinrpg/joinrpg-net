using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.Notifications;

[method: JsonConstructor]
[TypedEntityId]
public partial record NotificationId(int Value);
