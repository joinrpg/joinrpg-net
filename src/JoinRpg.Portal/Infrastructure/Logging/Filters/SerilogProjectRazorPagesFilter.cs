using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace JoinRpg.Portal.Infrastructure.Logging.Filters;

public class SerilogProjectRazorPagesFilter(IDiagnosticContext diagnosticContext) : IPageFilter
{
    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
        if (context.HttpContext.Items.TryGetValue(DiscoverFilters.Constants.ProjectIdName, out var projectId))
        {
            diagnosticContext.Set("ProjectId", projectId);
        }
    }

    // Required by the interface
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context) { }
}
