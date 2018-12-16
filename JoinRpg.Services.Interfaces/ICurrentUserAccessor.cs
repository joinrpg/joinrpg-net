using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces
{
    /// <summary>
    /// This interface supplying user id from 
    /// Should be implemented by application
    /// In ASP.NET it could be implemented by reading HttpContext.CurrentUser.
    /// In ASP.NET Core it will be more tricky
    /// </summary>
    public interface ICurrentUserAccessor
    {
        int CurrentUserId { get; }

    }

    public interface ICurrentUserInfo
    {
        int UserId { get; }
        string DisplayName { get; }
        string Email { get; }
    }
}
