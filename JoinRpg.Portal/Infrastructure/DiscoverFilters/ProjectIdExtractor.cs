using System.Linq;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Portal.Infrastructure.DiscoverFilters
{
    internal static class ProjectIdExtractor
    {
        public static int? TryExtractFromQuery(this IQueryCollection query)
        {
            if (query.ContainsKey(Constants.ProjectIdName)
                && int.TryParse(query[Constants.ProjectIdName], out var projectId))
            {
                return projectId;
            }
            else
            {
                return null;
            }
        }

        public static int? TryExtractFromPath(this PathString path)
        {
            if (!path.HasValue)
            {
                return null;
            }
            var parts = path.Value.Split('/');
            var projectIdAsString = parts.Skip(1).FirstOrDefault();
            if (projectIdAsString != null && int.TryParse(projectIdAsString, out var projectId))
            {
                return projectId;
            }
            else
            {
                return null;
            }
        }
    }
}
