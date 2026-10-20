using JoinRpg.Common.WebInfrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Error;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class ErrorPageModel(ILogger<ErrorPageModel> logger) : PageModel
{
    // Не OnGet/OnPost: страница перезапускается тем же методом, что и упавший запрос, а для
    // DELETE, PUT, OPTIONS и прочих обработчика нет — Razor Pages рендерит страницу без него.
    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        // При перезапуске UseStatusCodePagesWithReExecute привязка маршрута не работает,
        // потому что endpoint routing уже отработал до него. Берём статус из ответа.
        HttpStatusCode ??= HttpContext.Response.StatusCode is var sc and > 0 ? sc : null;

        if (HttpContext.Features.Get<IExceptionHandlerPathFeature>() is IExceptionHandlerPathFeature exceptionHandlerPathFeature)
        {
            if (exceptionHandlerPathFeature.Error is ProjectNotFoundException notFound)
            {
                IsProjectNotFound = true;
                logger.LogInformation("Запрошен несуществующий проект {notFoundProjectId} в {errorPath}",
                    notFound.ProjectId.Value,
                    exceptionHandlerPathFeature.Path);
            }
            else
            {
                logger.LogError(exceptionHandlerPathFeature.Error,
                    "Exception during web request in {errorPath}",
                    exceptionHandlerPathFeature.Path);
            }
        }
        else if (HttpStatusCode is null)
        {
            logger.LogError("It's suspicios that we hit error handler w/o exception");
        }

        SupportInfo = ErrorSupportInfo.FromHttpContext(HttpContext);
    }

    [BindProperty(SupportsGet = true)]
    public int? HttpStatusCode { get; set; }

    public bool IsProjectNotFound { get; private set; }

    public ErrorSupportInfo SupportInfo { get; private set; } = null!;
}
