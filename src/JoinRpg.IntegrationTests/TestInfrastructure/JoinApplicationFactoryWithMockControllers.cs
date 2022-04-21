using JoinRpg.IntegrationTest.TestInfrastructure.MockControllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.IntegrationTests.TestInfrastructure;

public class JoinApplicationFactoryWithMockControllers : JoinApplicationFactory
{
    protected override IHostBuilder CreateHostBuilder()
    {
        var builder = base.CreateHostBuilder();
        return builder
            .ConfigureServices(services =>
            {
                _ = services.AddControllersWithViews().AddApplicationPart(typeof(JoinApplicationFactory).Assembly);
                _ = services.AddTransient<MockDateTimeController>();
            });
    }
}
