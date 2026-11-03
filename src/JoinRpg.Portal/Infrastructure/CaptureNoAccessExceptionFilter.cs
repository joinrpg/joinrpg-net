using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace JoinRpg.Portal.Infrastructure;

/// <summary>
/// Ловит <see cref="NoAccessToProjectException"/> и <see cref="ProjectDeactivatedException"/> и показывает
/// страницу ошибки вместо 500. На контроллерах — через <see cref="IAsyncExceptionFilter"/>, на Razor Pages
/// exception-фильтры не выполняются, поэтому там — обёртка над <c>next()</c> в <see cref="IAsyncPageFilter"/> (#5983).
/// </summary>
public class CaptureNoAccessExceptionFilter : IAsyncExceptionFilter, IAsyncPageFilter, IOrderedFilter
{
    // Convention-фильтр с Order 0 оказался бы внутри [ProjectShouldBeActive] (атрибут страницы идёт раньше
    // при равном Order) — его обёртка над next() не увидела бы исключения. -1000 держит фильтр внешним.
    public int Order => -1000;

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        try
        {
            _ = await next();
        }
        catch (NoAccessToProjectException exception)
        {
            context.Result = await CreateNoAccessToProjectView(exception, context.HttpContext);
        }
        catch (ProjectDeactivatedException exception)
        {
            context.Result = CreateNotActiveProjectView(exception);
        }
    }

    public async Task OnExceptionAsync(ExceptionContext filterContext)
    {
        if (filterContext.Exception is NoAccessToProjectException noAccessException)
        {
            filterContext.Result = await CreateNoAccessToProjectView(noAccessException, filterContext.HttpContext);
        }


        if (filterContext.Exception is ProjectDeactivatedException projectDeactivatedException)
        {
            filterContext.Result = CreateNotActiveProjectView(projectDeactivatedException);
        }
    }

    private static async Task<ViewResult> CreateNoAccessToProjectView(NoAccessToProjectException exception, HttpContext httpContext)
    {
        var viewResult = new ViewResult()
        {
            ViewName = "ErrorNoAccessToProject",
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
        viewResult.ViewData[Constants.ProjectIdName] = exception.ProjectId.Value;
        var projectMetadataRepository = httpContext.RequestServices.GetRequiredService<IProjectMetadataRepository>();
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(exception.ProjectId);
        viewResult.ViewData.Model = NoAccessToProjectViewModelBuilder.Build(
            projectInfo,
            exception.UserId is int userId ? new UserIdentification(userId) : null,
            exception.Permission);
        return viewResult;
    }

    private static ViewResult CreateNotActiveProjectView(ProjectDeactivatedException exception)
    {
        var viewResult = new ViewResult()
        {
            ViewName = "ErrorNotActiveProject",
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
        viewResult.ViewData[Constants.ProjectIdName] = exception.ProjectId.Value;
        viewResult.ViewData["ProjectBlocked"] = exception is ProjectBlockedException;
        return viewResult;
    }
}
