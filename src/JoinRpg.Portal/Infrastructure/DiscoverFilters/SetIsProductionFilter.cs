using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace JoinRpg.Portal.Infrastructure;

/// <summary>
/// Sets ViewBag.IsProduction
/// </summary>
public class SetIsProductionFilterAttribute : ResultFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ViewResult viewResult)
        {
            SetIsProductionToViewData(context, viewResult.ViewData);
        }
        else if (context.Result is PageResult pageResult)
        {
            SetIsProductionToViewData(context, pageResult.ViewData);
        }
    }

    private static void SetIsProductionToViewData(ResultExecutingContext context, ViewDataDictionary viewData)
    {
        var host = context.HttpContext.Request.Host.Host;
        viewData["IsProduction"] = host == "joinrpg.ru";
        viewData["FullHostName"] = context.HttpContext.Request.Scheme + host;
    }
}
