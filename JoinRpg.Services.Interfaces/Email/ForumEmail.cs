using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Email
{
    public class ForumEmail : EmailModelBase
    {
        public ForumThread ForumThread { get; set; }
    }
}
