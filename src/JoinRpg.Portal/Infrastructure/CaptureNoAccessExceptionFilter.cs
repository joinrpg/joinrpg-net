using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace JoinRpg.Portal.Infrastructure;

public class CaptureNoAccessExceptionFilter(IProjectMetadataRepository projectMetadataRepository) : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext filterContext)
    {
        if (filterContext.Exception is NoAccessToProjectException noAccessException)
        {
            var viewResult = new ViewResult()
            {
                ViewName = "ErrorNoAccessToProject",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            };
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(noAccessException.ProjectId);
            viewResult.ViewData.Model = new ErrorNoAccessToProjectViewModel(projectInfo, noAccessException.Permission);
            filterContext.Result = viewResult;
        }
    }
}
