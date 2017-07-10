using System.Web.Http;

namespace JoinRpg.Web
{
  public static class WebApiConfig
  {
    public static void Register(HttpConfiguration config)
    {
      config.MapHttpAttributeRoutes();
      var container = UnityConfig.GetConfiguredContainer();
      config.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);
    }
  }
}