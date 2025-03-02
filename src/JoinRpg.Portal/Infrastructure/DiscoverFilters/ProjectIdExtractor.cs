using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Portal.Infrastructure.DiscoverFilters;

internal static class ProjectIdExtractor
{

    public static ProjectIdentification? TryGetProjectIdFromItems(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(Constants.ProjectIdName, out var projectIdBoxed))
        {
            if (projectIdBoxed is int projectId)
            {
                return new(projectId);
            }
        }
        return null;
    }

    public static ProjectIdentification? TryExtractFromQuery(this IQueryCollection query)
    {
        if (query.ContainsKey(Constants.ProjectIdName))
        {
            if (int.TryParse(query[Constants.ProjectIdName], out var projectId))
            {
                return new(projectId);
            }
            else if (ProjectIdentification.TryParse(query[Constants.ProjectIdName], null, out var projectId2))
            {
                return projectId2;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    public static ProjectIdentification? TryExtractFromPath(this PathString path)
    {
        if (path.Value is null)
        {
            return null;
        }
        var parts = path.Value.Split('/');
        var secondPart = parts.Skip(1).FirstOrDefault();
        if (secondPart != null && int.TryParse(secondPart, out var projectId))
        {
            return new(projectId);
        }
        else if (parts.Skip(2).FirstOrDefault() is string thirdPart && secondPart == "x-game-api" && int.TryParse(thirdPart, out var projectIdSecond))
        {
            return new(projectIdSecond);
        }
        else
        {
            return null;
        }
    }
}
