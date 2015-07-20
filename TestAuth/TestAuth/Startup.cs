using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TestAuth.Startup))]
namespace TestAuth
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
