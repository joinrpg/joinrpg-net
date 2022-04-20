using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Portal.Infrastructure.Authentication;

namespace JoinRpg.Portal.Controllers.Common
{
    /// <summary>
    /// This controller is used if we need to access user methods.
    /// It's recommended that you inject ICurrentUserAccessor in your constructor
    /// </summary>
    public abstract class LegacyJoinControllerBase : ControllerBase
    {

        protected readonly IUserRepository UserRepository;

        protected LegacyJoinControllerBase(IUserRepository userRepository) => UserRepository = userRepository;

        protected async Task<User> GetCurrentUserAsync() => await UserRepository.GetById(CurrentUserId);

        protected int CurrentUserId
        {
            get
            {
                var id = CurrentUserIdOrDefault;
                if (id == null)
                {
                    throw new Exception("Authorization required here");
                }

                return id.Value;
            }
        }

        protected int? CurrentUserIdOrDefault => User.GetUserIdOrDefault();

        //protected IReadOnlyDictionary<int, string> GetDynamicValuesFromPost(string prefix)
        //{
        //    //Some other fields can be [AllowHtml] so we need to use Request.Unvalidated.Form, or validator will fail.
        //    var post = Request.Form.ToDictionary();
        //    return post.Keys.UnprefixNumbers(prefix)
        //        .ToDictionary(fieldClientId => fieldClientId,
        //            fieldClientId => post[prefix + fieldClientId]);
        //}

        //private bool IsClientCached(DateTime contentModified)
        //{
        //    string header = Request.Headers["If-Modified-Since"];

        //    if (header == null) return false;

        //    return DateTime.TryParse(header, out var isModifiedSince) &&
        //           isModifiedSince.ToUniversalTime() > contentModified;
        //}

        //protected bool CheckCache(DateTime characterTreeModifiedAt)
        //{
        //    if (IsClientCached(characterTreeModifiedAt)) return true;
        //    Response.AddHeader("Last-Modified", characterTreeModifiedAt.ToString("R"));
        //    return false;
        //}

        //protected static HttpStatusCodeResult NotModified()
        //{
        //    return new HttpStatusCodeResult(304, "Page has not been modified");
        //}

        //protected string GetFullyQualifiedUri([AspMvcAction]
        //    string actionName,
        //    [AspMvcController]
        //    string controllerName,
        //    object routeValues)
        //{
        //    return Request.Scheme + "://" + Request.Host +
        //           Url.Action(actionName, controllerName, routeValues);
        //}

        //protected bool IsCurrentUserAdmin() => User.IsInRole(Security.AdminRoleName);
    }
}
