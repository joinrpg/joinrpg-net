using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace JoinRpg.Portal.Infrastructure.Logging.Filters;

public class XApiRequestLoggingFilter(IDiagnosticContext diagnosticContext) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        diagnosticContext.Set("XApiRequestArguments", context.ActionArguments, destructureObjects: true);
    }

    // Required by the interface
    public void OnActionExecuted(ActionExecutedContext context) { }
}
