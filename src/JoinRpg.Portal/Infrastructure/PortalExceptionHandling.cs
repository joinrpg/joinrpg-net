using Microsoft.AspNetCore.Diagnostics;

namespace JoinRpg.Portal.Infrastructure;

/// <summary>
/// Настройки <c>UseExceptionHandler</c> портала: какие исключения — штатный отказ, а не сбой.
/// </summary>
internal static class PortalExceptionHandling
{
    public const string ErrorPagePath = "/error";

    public static ExceptionHandlerOptions CreateOptions() => new()
    {
        ExceptionHandlingPath = ErrorPagePath,
        StatusCodeSelector = GetStatusCode,
        SuppressDiagnosticsCallback = ShouldSuppressDiagnostics,
    };

    /// <summary>
    /// Ссылка на несуществующий проект (боты, старые ссылки) — это «не найдено», а не падение сервера (#5709).
    /// </summary>
    public static bool IsNotFound(Exception exception) => exception is ProjectNotFoundException;

    public static int GetStatusCode(Exception exception)
        => IsNotFound(exception) ? StatusCodes.Status404NotFound : StatusCodes.Status500InternalServerError;

    /// <summary>
    /// По умолчанию обработанное в IExceptionHandler не логируется вовсе — а сбои в /webapi/ должны попадать в логи.
    /// Не найденный проект не логируем как ошибку ни на каком пути, но только если страница ошибки отрисовалась.
    /// </summary>
    public static bool ShouldSuppressDiagnostics(ExceptionHandlerSuppressDiagnosticsContext context)
        => context.ExceptionHandledBy switch
        {
            ExceptionHandledType.Unhandled => false,
            ExceptionHandledType.ExceptionHandlerService => WebApiExceptionHandler.IsExpectedRefusal(context.Exception),
            _ => IsNotFound(context.Exception),
        };
}
