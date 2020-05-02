using JoinRpg.Portal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTests.TestInfrastructure
{
    public class JoinApplicationFactory : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("IntegrationTest");
        }
    }
}
