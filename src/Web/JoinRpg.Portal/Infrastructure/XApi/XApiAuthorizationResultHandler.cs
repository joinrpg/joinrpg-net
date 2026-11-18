using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Infrastructure.XApi;

/// <summary>
/// Внешнему API (<c>/x-api</c>, <c>/x-game-api</c>) отказы авторизации (401/403) отдаёт как
/// <see cref="ProblemDetails"/>, а не пустым телом: машинный клиент получает машиночитаемую причину.
/// Прочие пути — прежнее поведение (острова /webapi и страницы не затрагиваются).
/// </summary>
internal class XApiAuthorizationResultHandler(IProblemDetailsService problemDetailsService) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded || !context.Request.Path.IsExternalApiPath())
        {
            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        // Сначала дефолт: ставит код (и WWW-Authenticate при challenge), не пишет тело.
        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);

        if (context.Response.HasStarted)
        {
            return;
        }

        var title = context.Response.StatusCode == StatusCodes.Status401Unauthorized
            ? "Токен доступа отсутствует, невалиден или истёк"
            : "Недостаточно прав для этого действия";
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = context.Response.StatusCode, Title = title },
        });
    }
}
