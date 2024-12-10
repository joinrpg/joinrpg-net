using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Data.Write.Interfaces.Notifications;

public class NotificationMessageDto
{
    public required MarkdownString Body { get; set; }
    public required UserIdentification Initiator { get; set; }

    public required Email InitiatorAddress { get; set; }
    public required string Header { get; set; }

    public required UserIdentification Recepient { get; set; }

    public required NotificationChannelDto[] Channels { get; set; }
}
