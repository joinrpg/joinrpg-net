using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Данные для техподдержки на странице ошибки: по ним ищется запрос в логах.
/// Показывает их компонент SupportInfoPanel из JoinRpg.Common.WebComponents.
/// </summary>
public record ErrorSupportInfo(
    string? ActivityId,
    string RequestId,
    string? Path,
    Uri? Referer,
    string PodName,
    AppVersionInfo Version)
{
    public static ErrorSupportInfo FromHttpContext(HttpContext httpContext)
    {
        // RawTarget — исходный адрес запроса, даже если страница ошибки отрисована через re-execute.
        var path = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget is { Length: > 0 } rawTarget
            ? httpContext.Request.Scheme + "://" + httpContext.Request.Host + rawTarget
            : null;

        return new ErrorSupportInfo(
            ActivityId: Activity.Current?.Id,
            RequestId: httpContext.TraceIdentifier,
            Path: path,
            Referer: httpContext.Request.GetTypedHeaders().Referer,
            PodName: Environment.MachineName,
            Version: AppVersionInfo.Current);
    }
}
