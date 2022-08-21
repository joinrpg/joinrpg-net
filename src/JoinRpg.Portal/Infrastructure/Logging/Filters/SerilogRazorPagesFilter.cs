using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace JoinRpg.Portal.Infrastructure.Logging.Filters;

public class SerilogRazorPagesFilter : IPageFilter
{
    private readonly IDiagnosticContext _diagnosticContext;
    public SerilogRazorPagesFilter(IDiagnosticContext diagnosticContext)
    {
        _diagnosticContext = diagnosticContext;
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
        var name = context.HandlerMethod?.Name ?? context.HandlerMethod?.MethodInfo.Name;
        if (name != null)
        {
            _diagnosticContext.Set("RazorPageHandler", name);
        }

        _diagnosticContext.Set("RouteData", context.ActionDescriptor.RouteValues);
        _diagnosticContext.Set("ActionName", context.ActionDescriptor.DisplayName);
        _diagnosticContext.Set("ActionId", context.ActionDescriptor.Id);
        _diagnosticContext.Set("ValidationState", context.ModelState.IsValid);
    }

    // Required by the interface
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context){}
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context) {}
}
