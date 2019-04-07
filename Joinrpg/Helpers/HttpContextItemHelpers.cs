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
            var projectIdRawValue = httpContext.GetRawValue(ProjectidKey);
            if (projectIdRawValue.GetType().IsArray)
            {
                return int.Parse(((string[])projectIdRawValue)[0]);
            }
            else
            {
                return int.Parse((string)projectIdRawValue);
            }
        }

        private static object GetRawValue(this HttpContextBase httpContext, string key)
        {
            return ((ValueProviderResult)httpContext.Items[key]).RawValue;
        }
    }
}
