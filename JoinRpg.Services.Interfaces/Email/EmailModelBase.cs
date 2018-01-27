using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces.Email
{
    public class EmailModelBase
    {
        public string ProjectName { get; set; }
        public User Initiator { get; set; }
        public MarkdownString Text { get; set; }
        public ICollection<User> Recipients { get; set; }
    }
}
