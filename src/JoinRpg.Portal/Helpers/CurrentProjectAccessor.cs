using System;
using JoinRpg.Portal.Helpers;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Web.Helpers
{
    public class CurrentProjectAccessor : ICurrentProjectAccessor
    {
        private readonly IHttpContextAccessor _contextAccessor;

        /// <summary>
        /// TODO we expect MasterAuthorize to put it for use, therefore is not usable for players'pages
        /// </summary>
        private readonly Lazy<int> ProjectIdLazy;

        public CurrentProjectAccessor(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
            ProjectIdLazy = new Lazy<int>(()
                => _contextAccessor.HttpContext?.GetProjectIdFromRouteOrDefault()
                ?? throw new Exception("Project id was not discovered, but master access required.That's probably problem with routing"));
        }

        public int ProjectId => ProjectIdLazy.Value;
    }
}
