using System.Web;
using System.Web.Mvc;

namespace JoinRpg.Web.Helpers
{
    /// <summary>
    /// Read something stored in HttpContext by attributes
    /// </summary>
    internal static class HttpContextItemHelpers
    {
        public const string ProjectidKey = "projectId";

        public static int GetProjectId(this HttpContextBase httpContext)
        {
            var item = httpContext.Items[ProjectidKey];
            if (item is int i)
            {
                return i;
            }
            var projectIdRawValue = ((ValueProviderResult)item).RawValue;
            if (projectIdRawValue.GetType().IsArray)
            {
                return int.Parse(((string[])projectIdRawValue)[0]);
            }
            else
            {
                return int.Parse((string)projectIdRawValue);
            }
        }
    }
}
