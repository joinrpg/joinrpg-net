using System;
using System.Web;
using JoinRpg.WebPortal.Managers.Interfaces;

namespace JoinRpg.Web.Helpers
{
    public class CurrentProjectAccessor : ICurrentProjectAccessor
    {
        private Lazy<int> ProjectIdLazy = new Lazy<int>(()
            => new HttpContextWrapper(HttpContext.Current).GetProjectId());

        public int ProjectId => ProjectIdLazy.Value;
    }
}
