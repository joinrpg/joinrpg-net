using System.Configuration;
using System.Data.Entity.Migrations;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.ApplicationInsights.Extensibility;

namespace JoinRpg.Web
{
  public class MvcApplication : System.Web.HttpApplication
  {
    protected void Application_Start()
    {
      AreaRegistration.RegisterAllAreas();
      FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
      RouteConfig.RegisterRoutes(RouteTable.Routes);
      BundleConfig.RegisterBundles(BundleTable.Bundles);

      var migrator = new DbMigrator(new Dal.Impl.Migrations.Configuration() {AutomaticMigrationsEnabled = false});
      migrator.Update();

      string telemetryKey = ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"];
      if (telemetryKey != null)
      {
        TelemetryConfiguration.Active.InstrumentationKey = telemetryKey;
      }
    }
  }
}