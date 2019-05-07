using System;
using System.Web;
using JoinRpg.WebPortal.Managers.Interfaces;

namespace JoinRpg.Web.Helpers
{
    public class CurrentProjectAccessor : ICurrentProjectAccessor
    {
        /// <summary>
        /// TODO we expect MasterAuthorize to put it for use, therefore is not usable for players'pages
        /// </summary>
        private Lazy<int> ProjectIdLazy = new Lazy<int>(()
            => new HttpContextWrapper(HttpContext.Current).GetProjectId());

        public int ProjectId => ProjectIdLazy.Value;
    }
}
