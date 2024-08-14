using Serilog.Context;

namespace JoinRpg.Portal.Infrastructure.DiscoverFilters;

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
        HttpRequest request = context.Request;

        if ((request.Path.TryExtractFromPath() ?? request.Query.TryExtractFromQuery()) is int projectId)
        {
            context.Items[Constants.ProjectIdName] = projectId;
            _ = LogContext.PushProperty(Constants.ProjectIdName, projectId);
        }

        await nextDelegate(context);
    }


}
