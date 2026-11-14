using JoinRpg.Common.WebInfrastructure.LeaderElection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.Test.LeaderElection;

/// <summary>
/// Перегрузка регистрации над <see cref="WebApplicationBuilder"/>: конфигурация берётся из builder'а (#6220).
/// </summary>
public class LeaderElectedRegistrationTest
{
    private sealed class SampleService : ILeaderElectedService
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public void BuilderOverloadRegistersHostedServiceAndLock()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DataProtection"] = "Host=localhost;Database=x",
        });

        builder.AddLeaderElectedHostedService<SampleService>();

        builder.Services.ShouldContain(d => d.ServiceType == typeof(IHostedService));
        builder.Services.ShouldContain(d => d.ServiceType == typeof(ILeaderLock));
    }

    [Fact]
    public void BuilderOverloadThrowsForMissingConnectionString()
    {
        var builder = WebApplication.CreateBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.AddLeaderElectedHostedService<SampleService>(connectionStringName: "DefinitelyMissing"));
    }
}
