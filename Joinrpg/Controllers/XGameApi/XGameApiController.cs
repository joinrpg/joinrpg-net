using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Helpers;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [SaveProjectIdAttribute]
  public class XGameApiController : ApiController
  {
    public XGameApiController(IProjectRepository projectRepository)
    {
      ProjectRepository = projectRepository;
    }

    public IProjectRepository ProjectRepository { get; }

    protected int GetCurrentUserId()
    {
      return int.Parse(User.Identity.GetUserId());
    }
  }

  public class SaveProjectIdAttribute : ActionFilterAttribute
  {
      public override void OnActionExecuting(HttpActionContext ctx)
      {
          var projectId = GetProjectId(ctx);
          if (projectId != null)
          {
              HttpContext.Current.Items[HttpContextItemHelpers.ProjectidKey] = projectId;
          }
          base.OnActionExecuting(ctx);
      }

      private static int? GetProjectId(HttpActionContext httpContext)
      {
          var routeDataValues = httpContext.ControllerContext.RouteData.Values;
          if (routeDataValues.ContainsKey(HttpContextItemHelpers.ProjectidKey))
          {

              var projectIdRawValue = routeDataValues["projectId"];
              if (projectIdRawValue.GetType().IsArray)
              {
                  return int.Parse(((string[])projectIdRawValue)[0]);
              }
              else
              {
                  return int.Parse((string)projectIdRawValue);
              }
          }
          return null;
      }
    }
}
