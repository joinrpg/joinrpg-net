using System.Threading.Tasks;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public class MasterAuthorize : TypeFilterAttribute
    {
        public Permission Permission { get; set; }

        public bool AllowPublish { get; set; }

        public bool AllowAdmin { get; set; }

        public MasterAuthorize(Permission permission = Permission.None)
            :base (typeof(MasterAuthorizeFilterImpl))
        {
            Arguments = new[] { this };
            Permission = permission;
        }

        private class MasterAuthorizeFilterImpl : IAsyncAuthorizationFilter
        {
            public Permission Permission { get; set; }

            public bool AllowPublish { get; set; }

            public bool AllowAdmin { get; set; }
            public IAuthorizationService Authorization { get; }

            public MasterAuthorizeFilterImpl(MasterAuthorize self, IAuthorizationService authorization)
            {
                Permission = self.Permission;
                AllowAdmin = self.AllowAdmin;
                AllowPublish = self.AllowPublish;
                Authorization = authorization;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                var user = context.HttpContext.User;
                if (AllowPublish && (await Authorization.AuthorizeAsync(user, "AllowPublish")).Succeeded)
                {
                    return;
                }
                if (AllowAdmin && (await Authorization.AuthorizeAsync(user, PolicyConstants.AllowAdminPolicy)).Succeeded)
                {
                    return;
                }
                if ((await Authorization.AuthorizeAsync(user, PolicyConstants.AllowMasterPolicyPrefix + Permission.ToString())).Succeeded)
                {
                    return;
                }
                context.Result = new ForbidResult();

                //var projectIdAsObj = context.HttpContext.Items["ProjectId"];
                //if (projectIdAsObj == null || !int.TryParse(projectIdAsObj.ToString(), out var projectId))
                //{
                //    context.
                //    return false;
                //}
                //var project = await ProjectRepository.GetProjectAsync(projectId);
                //var project = LoadProject(httpContext);

                //if (project == null)
                //{
                //    return false;
                //}

                //if (AllowPublish && project.Details.PublishPlot)
                //{
                //    return true;
                //}

                //var userId = httpContext.User.Identity.GetUserId();

                //if (AllowAdmin && httpContext.User.IsInRole(Security.AdminRoleName))
                //{
                //    return true;
                //}

                //return project.ProjectAcls.Any(acl => acl.UserId.ToString() == userId && Permission.GetPermssionExpression()(acl));
            }
        }
    
    private const string ProjectrepositoryKey = "projectRepository";

    //public override void OnAuthorization(AuthorizationContext filterContext)
    //{
    //  var httpContextItems = filterContext.HttpContext.Items;

    //  httpContextItems[HttpContextItemHelpers.ProjectidKey] =
    //            filterContext.Controller.ValueProvider.GetValue("ProjectId");
    //  httpContextItems[ProjectrepositoryKey] =
    //            ((ControllerGameBase) filterContext.Controller).ProjectRepository;

    //  base.OnAuthorization(filterContext);
    //}

            
    //protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
    //{
    //  if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
    //  {
    //    base.HandleUnauthorizedRequest(filterContext);
    //    return;
    //  }

    //  var project = LoadProject(filterContext.HttpContext);
    //  if (project == null)
    //  {
    //    filterContext.Result = new HttpNotFoundResult();
    //  }
    //  else
    //  {
    //    filterContext.Result = new ViewResult()
    //    {
    //      ViewName = "ErrorNoAccessToProject",
    //      ViewData = new ViewDataDictionary(new ErrorNoAccessToProjectViewModel(project, Permission)),
    //    };
    //  }
    //}
      
    }
}
