using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.OAuthServer;

/// <summary>
/// Необработанное исключение на протокольном пути (<see cref="OAuthServerRegistration.IsProtocolPath"/>)
/// отдаёт ошибкой протокола OAuth (<c>{"error":"server_error"}</c>), а не HTML-страницей ошибки:
/// её читает программа — OAuth-клиент или Portal при интроспекции токена (#5626).
/// Остальные пути не трогает — для них остаётся страница <c>/Error</c>.
/// </summary>
internal class OAuthProtocolExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!OAuthServerRegistration.IsProtocolPath(httpContext.Request.Path))
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.Headers.CacheControl = "no-store";
        await httpContext.Response.WriteAsJsonAsync(new Dictionary<string, string>
        {
            [Parameters.Error] = Errors.ServerError,
            // RFC 6749 §5.2 разрешает в error_description только ASCII, поэтому не по-русски.
            [Parameters.ErrorDescription] = "An unexpected server error occurred.",
        }, cancellationToken);
        return true;
    }
}
