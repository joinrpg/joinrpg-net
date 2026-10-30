using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Web.AdminTools;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Infrastructure;

/// <summary>
/// Ошибки API островов (<c>/webapi/</c>) отдаёт как <see cref="ProblemDetails"/>, а не HTML-страницей ошибки:
/// остров показывает <see cref="ProblemDetails.Title"/> пользователю (#4231).
/// Остальные пути не трогает — для них остаётся страница <c>/error</c>.
/// </summary>
internal class WebApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!GetOriginalPath(httpContext).IsInternalApiPath())
        {
            return false;
        }

        var (status, title) = GetRefusal(exception) ?? (StatusCodes.Status500InternalServerError, "Неизвестная ошибка на сервере");
        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title },
        });
    }

    /// <summary>
    /// Путь, на котором случилось исключение. <c>UseExceptionHandler</c> с путём страницы ошибки переписывает
    /// <see cref="HttpRequest.Path"/> на <c>/error</c> ещё до вызова обработчиков, поэтому исходный путь — только в
    /// <see cref="IExceptionHandlerPathFeature"/>. По <see cref="HttpRequest.Path"/> обработчик не узнавал /webapi
    /// никогда, и штатные отказы уходили 500-й с HTML-страницей.
    /// </summary>
    private static PathString GetOriginalPath(HttpContext httpContext)
        => httpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path is { } path
            ? new PathString(path)
            : httpContext.Request.Path;

    /// <summary>
    /// Штатный отказ в операции — не сбой сервера, его не надо логировать как необработанное исключение.
    /// </summary>
    public static bool IsExpectedRefusal(Exception exception) => GetRefusal(exception) is not null;

    private static (int Status, string Title)? GetRefusal(Exception exception) => exception switch
    {
        ProjectBlockedException => (StatusCodes.Status409Conflict, "Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны"),
        ProjectDeactivatedException => (StatusCodes.Status409Conflict, "Проект находится в архиве, изменения в нём невозможны"),
        NoAccessToProjectException => (StatusCodes.Status403Forbidden, "Недостаточно прав для этого действия"),
        ProjectNotFoundException => (StatusCodes.Status404NotFound, "Проект не найден"),
        DefaultTemplateMustAcceptClaimsException => (StatusCodes.Status409Conflict, "Шаблон по умолчанию должен принимать заявки игроков — выберите другой шаблон или откройте этот для заявок"),
        ParentCommentNotFoundException => (StatusCodes.Status404NotFound, "Комментарий, на который вы отвечаете, не найден. Обновите страницу и попробуйте ещё раз."),
        // Текст отказа собирается в админском сервисе и уже на русском
        UserAdminActionRefusedException refused => (StatusCodes.Status400BadRequest, refused.Message),
        VisibleReplyToHiddenCommentException => (StatusCodes.Status409Conflict, "Ответ на комментарий, скрытый от игрока, тоже должен быть скрыт от игрока"),
        JoinRpgNameFieldShouldBePublicException => (StatusCodes.Status409Conflict, "Поле, привязанное к имени персонажа, должно быть публичным (видно всем): имена персонажей показываются всем посетителям сайта"),
        _ => null,
    };
}
