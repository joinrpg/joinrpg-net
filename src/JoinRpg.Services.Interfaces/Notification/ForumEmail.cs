using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Forums;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Services.Interfaces.Notification;

[Obsolete]
public class ForumEmail : EmailModelBase
{
    public ForumThread ForumThread { get; set; }
}

public record class ForumMessageNotification(
    ForumCommentIdentification ForumCommentId,
    UserInfoHeader Initiator,
    MarkdownString Text,
    string Header
    );
