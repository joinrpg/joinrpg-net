using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Portal.Infrastructure.DiscoverFilters
{
    /// <summary>
    /// Store projectId for CurrentProjectAccessor
    /// </summary>
    public class DiscoverProjectMiddleware
    {
        private readonly RequestDelegate nextDelegate;

        public DiscoverProjectMiddleware(RequestDelegate nextDelegate) => this.nextDelegate = nextDelegate;

        /// <inheritedoc />
        public async Task InvokeAsync(HttpContext context)
        {
            var httpContextItems = context.Items;

            if (context.Request.Path.TryExtractFromPath() is int projectIdFromPath)
            {
                httpContextItems[Constants.ProjectIdName] = projectIdFromPath;
            }
            else if (context.Request.Query.TryExtractFromQuery() is int projectIdFromQuery)
            {
                httpContextItems[Constants.ProjectIdName] = projectIdFromQuery;
            }

            await nextDelegate(context);
        }


    }
}
