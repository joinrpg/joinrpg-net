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

        var pathSpan = path.Value.AsSpan();
        foreach (var segmentRange in pathSpan.Split('/'))
        {
            if (segmentRange.Start.Value == 0 && segmentRange.End.Value == 0) // пропускаем первый пустой
            {
                continue;
            }

            if (ApiPathHelper.IsApiPathSegment(pathSpan[segmentRange]))
            {
                continue; // пропускаем префикс API
            }

            if (ProjectIdentification.TryParse(pathSpan[segmentRange], null, out var projectId))
            {
                return projectId;
            }
            return null;
        }

        return null;
    }
}
