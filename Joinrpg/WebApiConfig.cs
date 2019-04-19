using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;

namespace JoinRpg.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
            var builder = new ContainerBuilder();
            builder.RegisterApiControllers(typeof(MvcApplication).Assembly);

            builder.RegisterModule<App_Start.WebModule>();


            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}
