using System.Web.Mvc;
using JoinRpg.Domain;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Filter
{
  public class JoinRpgExceptionHandlerAttribute : HandleErrorAttribute
  {
    #region Overrides of HandleErrorAttribute

    public override void OnException(ExceptionContext filterContext)
    {
      var noAccess = filterContext.Exception as NoAccessToProjectException;
      if (noAccess != null)
      {
         filterContext.Result = new ViewResult()
        {

          ViewName = "ErrorNoAccessToProject",
          ViewData = new ViewDataDictionary(new ErrorNoAccessToProjectViewModel(noAccess.Project)),
        };
      }
      base.OnException(filterContext);
    }

    #endregion
  }
}