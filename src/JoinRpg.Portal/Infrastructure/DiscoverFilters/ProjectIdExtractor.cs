namespace JoinRpg.Portal.Infrastructure.DiscoverFilters;

internal static class ProjectIdExtractor
{

    public static int? TryGetProjectIdFromItems(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(Constants.ProjectIdName, out var projectIdBoxed))
        {
            if (projectIdBoxed is int projectId)
            {
                return projectId;
            }
        }
        return null;
    }

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
        if (path.Value is null)
        {
            return null;
        }
        var parts = path.Value.Split('/');
        var secondPart = parts.Skip(1).FirstOrDefault();
        if (secondPart != null && int.TryParse(secondPart, out var projectId))
        {
            return projectId;
        }
        else if (parts.Skip(2).FirstOrDefault() is string thirdPart && secondPart == "x-game-api" && int.TryParse(thirdPart, out var projectIdSecond))
        {
            return projectIdSecond;
        }
        else
        {
            return null;
        }
    }
}
