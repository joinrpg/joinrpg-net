using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure;

public class RedirectAntiforgeryValidationFailedResultFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is IAntiforgeryValidationFailedResult)
        {
            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(context.ActionDescriptor.DisplayName ?? nameof(RedirectAntiforgeryValidationFailedResultFilter));
            logger.LogWarning("Antiforgery validation error");
            // Острова Blazor ходят в /webapi через fetch, а он молча идёт по редиректу и получает
            // 200 со страницей ошибки: клиент принял бы отказ за успех и не откатил бы изменение.
            // Поэтому для API — код ошибки с причиной, редирект — только для страниц.
            context.Result = context.HttpContext.Request.Path.IsInternalApiPath()
                ? new BadRequestObjectResult("Сессия устарела — обновите страницу")
                : new RedirectResult("/error/antiforgery");
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
