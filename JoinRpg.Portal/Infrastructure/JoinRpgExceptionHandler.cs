using JoinRpg.Domain;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Web.Filter
{
    public class JoinRpgExceptionHandlerAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            var noAccess = filterContext.Exception as NoAccessToProjectException;
            if (noAccess != null)
            {
                var viewResult = new ViewResult()
                {
                    ViewName = "ErrorNoAccessToProject",
                };
                viewResult.ViewData.Model = new ErrorNoAccessToProjectViewModel(noAccess.Project);
                filterContext.Result = viewResult;
            }
            base.OnException(filterContext);
        }
    }
}
