using Microsoft.AspNet.Identity;

namespace Joinrpg.Web.Identity
{
    public class IdentityUser : IUser<int>
    {
        public int Id { get; internal set; }
        public string UserName { get; set; }

        public int UserId => Id; //To facilitate move, remove later

        public bool HasPassword { get; internal set; }
    }
}
