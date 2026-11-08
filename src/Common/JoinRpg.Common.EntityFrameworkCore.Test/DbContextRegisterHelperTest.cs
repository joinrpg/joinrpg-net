using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.EntityFrameworkCore.Test;

public class DbContextRegisterHelperTest
{
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);

    [Fact]
    public void RegistersHealthCheckWithReadyTagByDefault()
    {
        var registration = RegisterHealthCheck(includeInReadyHealthCheck: null);

        registration.Name.ShouldBe("TestDb-db");
        registration.Tags.ShouldBe(["ready"]);
    }

    [Fact]
    public void ReadyTagCanBeOptedOut()
    {
        var registration = RegisterHealthCheck(includeInReadyHealthCheck: false);

        registration.Tags.ShouldNotContain("ready");
    }

    private static HealthCheckRegistration RegisterHealthCheck(bool? includeInReadyHealthCheck)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TestDb"] = "Host=localhost;Database=test;Username=test;Password=test",
            })
            .Build();

        var services = new ServiceCollection();
        if (includeInReadyHealthCheck is { } include)
        {
            services.AddJoinEfCoreDbContext<TestDbContext>(
                configuration,
                new TestHostEnvironment(),
                "TestDb",
                optionsBuilder: null,
                includeInReadyHealthCheck: include);
        }
        else
        {
            services.AddJoinEfCoreDbContext<TestDbContext>(
                configuration,
                new TestHostEnvironment(),
                "TestDb");
        }

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        return options.Registrations.ShouldHaveSingleItem();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
