using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace JoinRpg.Portal.Infrastructure.Logging.Filters;

public class SerilogMvcFilter : IActionFilter
{
    private readonly IDiagnosticContext _diagnosticContext;
    public SerilogMvcFilter(IDiagnosticContext diagnosticContext)
    {
        _diagnosticContext = diagnosticContext;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        _diagnosticContext.Set("RouteData", context.ActionDescriptor.RouteValues);
        _diagnosticContext.Set("ActionName", context.ActionDescriptor.DisplayName);
        _diagnosticContext.Set("ActionId", context.ActionDescriptor.Id);
        _diagnosticContext.Set("ValidationState", context.ModelState.IsValid);
    }

    // Required by the interface
    public void OnActionExecuted(ActionExecutedContext context) { }
}
