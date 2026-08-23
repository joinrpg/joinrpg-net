using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace JoinRpg.Common.WebInfrastructure.Logging.Filters;

public class SerilogMvcFilter(IDiagnosticContext diagnosticContext) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        diagnosticContext.Set("RouteData", context.ActionDescriptor.RouteValues);
        diagnosticContext.Set("ActionName", context.ActionDescriptor.DisplayName);
        diagnosticContext.Set("ActionId", context.ActionDescriptor.Id);
        diagnosticContext.Set("ValidationState", context.ModelState.IsValid);
    }

    // Required by the interface
    public void OnActionExecuted(ActionExecutedContext context) { }
}
