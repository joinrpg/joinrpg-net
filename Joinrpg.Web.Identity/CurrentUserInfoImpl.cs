using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.Services.Interfaces;

namespace Joinrpg.Web.Identity
{
    internal class CurrentUserInfoImpl : ICurrentUserInfo
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
    }
}
