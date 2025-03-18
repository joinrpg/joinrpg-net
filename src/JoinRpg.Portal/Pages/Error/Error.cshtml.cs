using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Error;

public class ErrorPageModel(ILogger<ErrorPageModel> logger) : PageModel
{
    public void OnGet() => FillErrorData();

    public void OnPost() => FillErrorData();

    private void FillErrorData()
    {
        if (HttpContext.Features.Get<IExceptionHandlerPathFeature>() is IExceptionHandlerPathFeature exceptionHandlerPathFeature)
        {
            logger.LogError(exceptionHandlerPathFeature.Error,
                "Exception during web request in {errorPath}",
                exceptionHandlerPathFeature.Path);
        }
        else if (HttpStatusCode is null)
        {
            logger.LogError("It's suspicios that we hit error handler w/o exception");
        }

        ActivityId = Activity.Current?.Id;
        RequestId = HttpContext.TraceIdentifier;
        if (HttpContext.Features.Get<IHttpRequestFeature>()?.RawTarget is string path)
        {
            Path = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + path;
        }
        else
        {
            Path = null;
        }
        Referer = HttpContext.Request.GetTypedHeaders().Referer;
    }

    [BindProperty(SupportsGet = true)]
    public int? HttpStatusCode { get; set; }
    public string? ActivityId { get; set; }

    public string? Path { get; set; }

    public required string RequestId { get; set; }
    public Uri? Referer { get; set; }
}
