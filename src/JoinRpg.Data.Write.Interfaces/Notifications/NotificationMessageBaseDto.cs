using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Data.Write.Interfaces.Notifications;

public abstract class NotificationMessageBaseDto
{
    public required MarkdownString Body { get; init; }
    public required UserIdentification Initiator { get; init; }
    public required Email InitiatorAddress { get; init; }
    public required string Header { get; init; }
    public required UserIdentification Recipient { get; init; }
}
