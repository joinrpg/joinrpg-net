using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace JoinRpg.Common.WebInfrastructure.Logging.Filters;

public class SerilogRazorPagesFilter(IDiagnosticContext diagnosticContext) : IPageFilter
{
    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
        var name = context.HandlerMethod?.Name ?? context.HandlerMethod?.MethodInfo.Name;
        if (name != null)
        {
            diagnosticContext.Set("RazorPageHandler", name);
        }

        diagnosticContext.Set("RouteData", context.ActionDescriptor.RouteValues);
        diagnosticContext.Set("ActionName", context.ActionDescriptor.DisplayName);
        diagnosticContext.Set("ActionId", context.ActionDescriptor.Id);
        diagnosticContext.Set("ValidationState", context.ModelState.IsValid);
    }

    // Required by the interface
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context) { }
}
