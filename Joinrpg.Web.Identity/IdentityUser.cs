using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;

namespace Joinrpg.Web.Identity
{
    public class IdentityUser : IUser<int>
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }
}
