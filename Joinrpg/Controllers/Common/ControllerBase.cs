using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using Microsoft.AspNet.Identity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Interfaces;

namespace JoinRpg.Web.Controllers.Common
{
    [ValidateInput(false)]
    public class ControllerBase : Controller
    {
        protected readonly ApplicationUserManager UserManager;
        protected readonly IUserRepository UserRepository;

        protected ControllerBase(ApplicationUserManager userManager, IUserRepository userRepository)
        {
            UserManager = userManager;
            UserRepository = userRepository;
        }

        //TODO move this to attribure
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.IsProduction = filterContext.HttpContext.Request.Url?.Host == "joinrpg.ru";
            //TODO inject this from DI
            ICurrentUserAccessor accessor = new CurrentUserAccessor();
            if (accessor.UserIdOrDefault != null)
            {
                ViewBag.UserDisplayName = accessor.DisplayName;
                ViewBag.GravatarHash = accessor.Email.GravatarHash().Trim();
            }

            base.OnActionExecuting(filterContext);
        }

        protected async Task<User> GetCurrentUserAsync()
        {
            return await UserRepository.GetById(CurrentUserId);
        }

        protected int CurrentUserId
        {
            get
            {
                var id = CurrentUserIdOrDefault;
                if (id == null)
                    throw new Exception("Authorization required here");
                return id.Value;
            }
        }

        protected int? CurrentUserIdOrDefault
        {
            get
            {
                var userIdString = User.Identity.GetUserId();
                if (userIdString == null)
                {
                    return null;
                }
                else
                {
                    return int.TryParse(userIdString, out var i) ? (int?)i : null;
                }
            }
        }

        protected IReadOnlyDictionary<int, string> GetDynamicValuesFromPost(string prefix)
        {
            //Some other fields can be [AllowHtml] so we need to use Request.Unvalidated.Form, or validator will fail.
            var post = Request.Unvalidated.Form.ToDictionary();
            return post.Keys.UnprefixNumbers(prefix)
                .ToDictionary(fieldClientId => fieldClientId,
                    fieldClientId => post[prefix + fieldClientId]);
        }

        private bool IsClientCached(DateTime contentModified)
        {
            string header = Request.Headers["If-Modified-Since"];

            if (header == null) return false;

            return DateTime.TryParse(header, out var isModifiedSince) &&
                   isModifiedSince.ToUniversalTime() > contentModified;
        }

        protected bool CheckCache(DateTime characterTreeModifiedAt)
        {
            if (IsClientCached(characterTreeModifiedAt)) return true;
            Response.AddHeader("Last-Modified", characterTreeModifiedAt.ToString("R"));
            return false;
        }

        protected static HttpStatusCodeResult NotModified()
        {
            return new HttpStatusCodeResult(304, "Page has not been modified");
        }

        protected string GetFullyQualifiedUri([AspMvcAction]
            string actionName,
            [AspMvcController]
            string controllerName,
            object routeValues)
        {
            if (Request.Url == null)
            {
                throw new InvalidOperationException("Request.Url is unexpectedly null");
            }

            return Request.Url.Scheme + "://" + Request.Url.Host +
                   (Request.Url.IsDefaultPort ? "" : $":{Request.Url.Port}") +
                   Url.Action(actionName, controllerName, routeValues);
        }

        protected bool IsCurrentUserAdmin() => User.IsInRole(Security.AdminRoleName);

        protected ActionResult ViewIfFound(string viewName, object model)
            => model == null ? (ActionResult)HttpNotFound() : View(viewName, model);

        protected ActionResult ViewIfFound(object model)
            => ViewIfFound(null, model);

        protected async Task<ActionResult> ViewIfFound<T>(Task<T> model)
            => ViewIfFound(null, await model);

        protected async Task<ActionResult> ViewIfFound<T>(string viewName, Task<T> model)
            => ViewIfFound(viewName, await model);
    }
}
