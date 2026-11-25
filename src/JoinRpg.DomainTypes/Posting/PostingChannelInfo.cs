using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.Posting;

[method: JsonConstructor]
[TypedEntityId(ShortName = "AdvChannel")]
public partial record PostingChannelIdentification(int Value);

public record PostingChannelInfo(
    PostingChannelIdentification ChannelId,
    string Name,
    ProjectIdentification? BoundProjectId, // null = глобальный канал
    PostingChannelSettings Settings,
    bool IsActive = true);
