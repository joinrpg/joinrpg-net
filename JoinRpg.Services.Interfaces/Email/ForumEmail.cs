using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public class ForumEmail : EmailModelBase
    {
        public ForumThread ForumThread { get; set; }
    }
}
