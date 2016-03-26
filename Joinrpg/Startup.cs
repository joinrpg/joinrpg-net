using JetBrains.Annotations;
using JoinRpg.Web;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]
namespace JoinRpg.Web
{
    public partial class Startup
    {
      [UsedImplicitly]
      public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
