using JoinRpg.PrimitiveTypes;
using Serilog.Context;

namespace JoinRpg.Portal.Infrastructure.DiscoverFilters;

/// <summary>
/// Store projectId for CurrentProjectAccessor
/// </summary>
public class DiscoverProjectMiddleware(RequestDelegate nextDelegate)
{

    /// <inheritedoc />
    public async Task InvokeAsync(HttpContext context)
    {
        HttpRequest request = context.Request;

        if ((request.Path.TryExtractFromPath() ?? request.Query.TryExtractFromQuery()) is ProjectIdentification projectId)
        {
            context.Items[Constants.ProjectIdName] = projectId.Value;
            _ = LogContext.PushProperty(Constants.ProjectIdName, projectId);
        }

        await nextDelegate(context);
    }


}
