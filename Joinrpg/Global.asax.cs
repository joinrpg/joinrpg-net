using System.Data.Entity.Migrations;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;

namespace JoinRpg.Web
{
    public class MvcApplication : System.Web.HttpApplication
  {
    protected void Application_Start()
    {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);
            //builder.RegisterApiControllers(typeof(MvcApplication).Assembly);


            builder.RegisterModule<App_Start.WebModule>();


            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

            GlobalConfiguration.Configure(WebApiConfig.Register);
      AreaRegistration.RegisterAllAreas();
      FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
      RouteConfig.RegisterRoutes(RouteTable.Routes);
      BundleConfig.RegisterBundles(BundleTable.Bundles);

      var migrator = new DbMigrator(new Dal.Impl.Migrations.Configuration());
      migrator.Update();
    }
  }
}
