using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace JoinRpg.Portal.Infrastructure.Logging.Filters;

public class SerilogProjectMvcFilter(IDiagnosticContext diagnosticContext) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Items.TryGetValue(DiscoverFilters.Constants.ProjectIdName, out var projectId))
        {
            diagnosticContext.Set("ProjectId", projectId);
        }
    }

    // Required by the interface
    public void OnActionExecuted(ActionExecutedContext context) { }
}
