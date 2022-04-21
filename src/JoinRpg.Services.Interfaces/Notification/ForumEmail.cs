using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Notification;

public class ForumEmail : EmailModelBase
{
    public ForumThread ForumThread { get; set; }
}
