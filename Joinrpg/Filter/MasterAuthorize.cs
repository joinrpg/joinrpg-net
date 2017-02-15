using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Filter
{

  public class MasterAuthorize : AuthorizeAttribute
  {
    private Permission Permission { get; }

    public MasterAuthorize(Permission permission = Permission.None)
    {
      Permission = permission;
    }

    private const string ProjectidKey = "projectId";
    private const string ProjectrepositoryKey = "projectRepository";

    public override void OnAuthorization(AuthorizationContext filterContext)
    {
      var projectId = filterContext.Controller.ValueProvider.GetValue("ProjectId");
      var projectRepository = ((ControllerGameBase) filterContext.Controller).ProjectRepository;
      filterContext.HttpContext.Items.Add(ProjectidKey, projectId);
      filterContext.HttpContext.Items.Add(ProjectrepositoryKey, projectRepository);
      base.OnAuthorization(filterContext);
    }

    protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
    {
      if (filterContext.HttpContext.User.Identity.IsAuthenticated)
      {
        var project = LoadProject(filterContext.HttpContext);
        filterContext.Result = new ViewResult()
        {

          ViewName = "ErrorNoAccessToProject",
          ViewData = new ViewDataDictionary(new ErrorNoAccessToProjectViewModel(project, Permission))
        };
      }
      else
      {
        base.HandleUnauthorizedRequest(filterContext);
      }
    }

    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
      return base.AuthorizeCore(httpContext) && AuthorizeMaster(httpContext);
    }

    private bool AuthorizeMaster(HttpContextBase httpContext)
    {
      var project = LoadProject(httpContext);

      var user = httpContext.User.Identity.GetUserId();
      
      return project.ProjectAcls.Any(acl => acl.UserId.ToString() == user && GetPermssionExpression()(acl));
    }

    private static Project LoadProject(HttpContextBase httpContext)
    {
      var projectId = int.Parse(((string[]) GetRawValue(httpContext, ProjectidKey))[0]);
      var repository = (IProjectRepository) httpContext.Items[ProjectrepositoryKey];
      var project = repository.GetProjectAsync(projectId).GetAwaiter().GetResult();
      return project;
    }

    [MustUseReturnValue]
    private Func<ProjectAcl, bool> GetPermssionExpression()
    {
      switch (Permission)
      {
        case Permission.CanChangeFields:
          return acl => acl.CanChangeFields;
        case Permission.CanChangeProjectProperties:
          return acl => acl.CanChangeProjectProperties;
        case Permission.CanGrantRights:
          return acl => acl.CanGrantRights;
        case Permission.CanManageClaims:
          return acl => acl.CanManageClaims;
        case Permission.CanEditRoles:
          return acl => acl.CanEditRoles;
        case Permission.CanManageMoney:
          return acl => acl.CanManageMoney;
        case Permission.CanSendMassMails:
          return acl => acl.CanSendMassMails;
        case Permission.CanManagePlots:
          return acl => acl.CanManagePlots;
        case Permission.None:
          return acl => true;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private static object GetRawValue(HttpContextBase httpContext, string key)
    {
      return ((ValueProviderResult) httpContext.Items[key]).RawValue;
    }
  }
}