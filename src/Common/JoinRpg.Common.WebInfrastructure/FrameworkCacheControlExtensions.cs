using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Долгоживущий кеш для фингерпринтных (content-addressed) файлов Blazor WebAssembly из <c>/_framework</c>.
/// </summary>
public static class FrameworkCacheControlExtensions
{
    /// <summary>
    /// Отдаёт фингерпринтные файлы <c>/_framework/*</c> (имя ассета содержит отпечаток содержимого,
    /// например <c>My.Assembly.a1b2c3d4e5.wasm</c>) с <c>Cache-Control: public, max-age=31536000, immutable</c>,
    /// минуя <c>MapStaticAssets</c>.
    /// </summary>
    /// <remarks>
    /// <c>MapStaticAssets</c> отдаёт нефингерпринтные ассеты (<c>blazor.web.js</c>, <c>dotnet.js</c> —
    /// имя не меняется между деплоями) с <c>no-cache</c> и полагается на ревалидацию 304. За L7-прокси
    /// ревалидация сломана (If-None-Match портится, If-Modified-Since игнорируется), поэтому при каждом
    /// открытии страницы с WASM-островом браузер заново качает мегабайты рантайма. Endpoint conventions
    /// и middleware после <c>MapStaticAssets().ShortCircuit()</c> не помогают: ShortCircuitMiddleware
    /// исполняет эндпоинт сразу после routing, а <c>StaticAssetsInvoker</c> безусловно перезаписывает
    /// Cache-Control при исполнении (заголовок, выставленный раньше или через OnStarting, теряется).
    /// Единственная надёжная точка — middleware до routing, отдающий файл сам.
    /// Точки входа (<c>blazor.web.js</c>, <c>dotnet.js</c>, <c>blazor.webassembly.js</c>) сознательно
    /// остаются <c>no-cache</c>: они меняются при каждом деплое без смены имени, и закешировать их
    /// навсегда нельзя. Файлы без фингерпринта в имени, как и всё вне <c>/_framework</c>, middleware
    /// пропускает дальше по конвейеру.
    /// Вызывается из <c>WebInfrastructureRegistration.AddJoinWebPlatform</c> (всех перегрузок).
    /// </remarks>
    public static IServiceCollection AddJoinFrameworkCacheControl(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, FrameworkCacheControlStartupFilter>());
        return services;
    }

    private const string FrameworkPathPrefix = "/_framework";
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    /// <summary>
    /// Отпечаток содержимого, который SDK ставит в имя ассета: ровно 10 символов [0-9a-z] между
    /// именем и расширением (<c>My.Assembly.a1b2c3d4e5.wasm</c>; проверено на выводе publish SDK 10 —
    /// все 200+ ассетов, включая буквенные отпечатки вроде <c>eqsisamrga</c>). Длина зафиксирована
    /// точно: диапазон «8–12» захватил бы <c>blazor.webassembly.js</c> (сегмент <c>webassembly</c>
    /// из 11 символов) — это точка входа, которая меняется между деплоями без смены имени.
    /// Если SDK изменит длину отпечатка, файлы молча останутся на no-cache — безопасное направление.
    /// </summary>
    private static bool HasContentFingerprint(string fileName)
    {
        var extensionDot = fileName.LastIndexOf('.');
        if (extensionDot <= 0)
        {
            return false;
        }

        var fingerprintDot = fileName.LastIndexOf('.', extensionDot - 1);
        if (fingerprintDot < 0)
        {
            return false;
        }

        const int fingerprintLength = 10;
        if (extensionDot - fingerprintDot - 1 != fingerprintLength)
        {
            return false;
        }

        for (var i = fingerprintDot + 1; i < extensionDot; i++)
        {
            var c = fileName[i];
            if (!((uint)(c - '0') <= 9 || (uint)(c - 'a') <= 25))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Сжатая версия файла лежит рядом (<c>name.wasm.br</c> / <c>name.wasm.gz</c>, их кладёт publish)
    /// и отдаётся, если клиент её принимает. Brotli предпочтительнее gzip.
    /// </summary>
    private static IFileInfo SelectFileToSend(IWebHostEnvironment environment, string subPath, IFileInfo fileInfo, IList<StringWithQualityHeaderValue>? acceptEncoding, out string? contentEncoding)
    {
        contentEncoding = null;
        if (acceptEncoding is not null)
        {
            foreach (var (extension, encoding) in new[] { (".br", "br"), (".gz", "gzip") })
            {
                var accepted = acceptEncoding.Any(a =>
                    string.Equals(a.Value.Value, encoding, StringComparison.OrdinalIgnoreCase)
                    && a.Quality.GetValueOrDefault(1.0) > 0);
                if (accepted)
                {
                    var compressedFileInfo = environment.WebRootFileProvider.GetFileInfo(subPath + extension);
                    if (compressedFileInfo.Exists)
                    {
                        contentEncoding = encoding;
                        return compressedFileInfo;
                    }
                }
            }
        }

        return fileInfo;
    }

    private sealed class FrameworkCacheControlStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            var contentTypes = new FileExtensionContentTypeProvider();
            contentTypes.Mappings[".wasm"] = "application/wasm";

            app.Use(async (context, nextMiddleware) =>
            {
                var request = context.Request;
                if ((!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
                    || !request.Path.StartsWithSegments(FrameworkPathPrefix, StringComparison.OrdinalIgnoreCase, out var remainder)
                    || string.IsNullOrEmpty(remainder.Value))
                {
                    await nextMiddleware(context);
                    return;
                }

                var fileSubPath = FrameworkPathPrefix[1..] + "/" + remainder.Value.TrimStart('/');
                var fileName = fileSubPath[(fileSubPath.LastIndexOf('/') + 1)..];
                if (!HasContentFingerprint(fileName))
                {
                    await nextMiddleware(context);
                    return;
                }

                var fileInfo = environment.WebRootFileProvider.GetFileInfo(fileSubPath);
                if (!fileInfo.Exists)
                {
                    await nextMiddleware(context);
                    return;
                }

                var response = context.Response;
                response.Headers.CacheControl = ImmutableCacheControl;
                response.ContentType = contentTypes.TryGetContentType(fileName, out var contentType)
                    ? contentType
                    : "application/octet-stream";

                response.Headers.Vary = HeaderNames.AcceptEncoding;

                var acceptEncoding = request.GetTypedHeaders().AcceptEncoding;
                var sendFileInfo = SelectFileToSend(environment, fileSubPath, fileInfo, acceptEncoding, out var contentEncoding);
                if (contentEncoding is not null)
                {
                    response.Headers.ContentEncoding = contentEncoding;
                }
                response.ContentLength = sendFileInfo.Length;
                response.Headers.LastModified = sendFileInfo.LastModified.ToString("R");

                if (HttpMethods.IsGet(request.Method))
                {
                    try
                    {
                        await response.SendFileAsync(sendFileInfo, context.RequestAborted);
                    }
                    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                    {
                        // Клиент отвалился посреди отдачи файла — как в StaticAssetsInvoker
                    }
                }
            });

            next(app);
        };
    }
}
