using System.Web.Http;
using Microsoft.Practices.Unity.WebApi;

namespace JoinRpg.Web
{
  public static class WebApiConfig
  {
    public static void Register(HttpConfiguration config)
    {
      config.MapHttpAttributeRoutes();
      var container = UnityConfig.GetConfiguredContainer();
      config.DependencyResolver = new UnityDependencyResolver(container);
    }
  }
}
