using System;
using JoinRpg.PrimitiveTypes;
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
            ProjectIdLazy = new Lazy<ProjectIdentification>(()
                => new((int)HttpContextAccessor.HttpContext!.Items[DiscoverFilters.Constants.ProjectIdName]!));
        }

        private readonly Lazy<ProjectIdentification> ProjectIdLazy;

        public ProjectIdentification ProjectId => ProjectIdLazy.Value;
    }
}
