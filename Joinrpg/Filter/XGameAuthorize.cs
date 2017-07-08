using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Controllers.XGameApi;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;
using AuthorizeAttribute = System.Web.Http.AuthorizeAttribute;

namespace JoinRpg.Web.Filter
{

  public class XGameAuthorize : AuthorizeAttribute
  {
    public Permission Permission { get; set; }

    public bool AllowAdmin { get; set; }

    public XGameAuthorize(Permission permission = Permission.None)
    {
      Permission = permission;
    }

    private const string ProjectidKey = "projectId";

    private static XGameApiController GetController(HttpActionContext actionContext)
    {
      return (XGameApiController)actionContext.ControllerContext.Controller;
    }

    protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
    {
      if (!GetController(actionContext).User.Identity.IsAuthenticated)
      {
        base.HandleUnauthorizedRequest(actionContext);
        return;
      }

      actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
    }

    public override async Task OnAuthorizationAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
    {
      var project = await LoadProject(actionContext);

      if (project == null)
      {
        HandleUnauthorizedRequest(actionContext);
        return;
      }

      var user = GetController(actionContext).User;
      var userId = user.Identity.GetUserId();

      if ((AllowAdmin && user.IsInRole(Security.AdminRoleName)) ||
          project.ProjectAcls.Any(acl => acl.UserId.ToString() == userId &&
                                         Permission.GetPermssionExpression()(acl)))
      {
        await base.OnAuthorizationAsync(actionContext, cancellationToken);
      }

      else
      {
        HandleUnauthorizedRequest(actionContext);
      }
    }

    [NotNull, ItemCanBeNull]
    private static async Task<Project> LoadProject(HttpActionContext httpContext)
    {
      var projectId = GetProjectId(httpContext);
      if (projectId == null)
      {
        return null;
      }
      var repository = GetController(httpContext).ProjectRepository;
      return await repository.GetProjectAsync((int) projectId);
    }

    private static int? GetProjectId(HttpActionContext httpContext)
    {
      var routeDataValues = httpContext.ControllerContext.RouteData.Values;
      if (routeDataValues.ContainsKey(ProjectidKey))
      {

        var projectIdRawValue = routeDataValues["projectId"];
        if (projectIdRawValue.GetType().IsArray)
        {
          return int.Parse(((string[]) projectIdRawValue)[0]);
        }
        else
        {
          return int.Parse((string) projectIdRawValue);
        }
      }
      return null;
    }
  }
}