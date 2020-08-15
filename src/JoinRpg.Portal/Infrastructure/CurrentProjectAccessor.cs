using System;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Portal.Infrastructure
{
    public class CurrentProjectAccessor : ICurrentProjectAccessor
    {
        private IHttpContextAccessor HttpContextAccessor { get; }

        public CurrentProjectAccessor(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
            ProjectIdLazy = new Lazy<int>(()
                => (int)HttpContextAccessor.HttpContext.Items[DiscoverFilters.Constants.ProjectIdName]);
        }

        private readonly Lazy<int> ProjectIdLazy;

        public int ProjectId => ProjectIdLazy.Value;
    }
}
