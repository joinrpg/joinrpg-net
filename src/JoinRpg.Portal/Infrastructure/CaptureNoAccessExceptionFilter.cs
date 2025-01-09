using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
            };
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(noAccessException.ProjectId);
            var masters = await projectMetadataRepository.GetMastersList(noAccessException.ProjectId);
            viewResult.ViewData.Model = new ErrorNoAccessToProjectViewModel(projectInfo, masters.Masters, noAccessException.Permission);
            filterContext.Result = viewResult;
        }
    }
}
