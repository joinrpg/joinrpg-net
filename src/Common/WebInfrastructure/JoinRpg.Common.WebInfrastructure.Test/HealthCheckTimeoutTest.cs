using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Регрессия на #4994: чеки, зарегистрированные без timeout:, висели столько, сколько сочтёт
/// нужным внешний сервис, и вместе с ними висел весь /health.
/// </summary>
public class HealthCheckTimeoutTest
{
    [Fact]
    public void CheckWithoutTimeoutGetsDefaultOne()
    {
        var registrations = BuildRegistrations(checks => checks
            .AddCheck("no-timeout", () => HealthCheckResult.Healthy()));

        registrations["no-timeout"].Timeout.ShouldBe(HealthCheckExtensions.DefaultHealthCheckTimeout);
    }

    [Fact]
    public void ExplicitTimeoutIsPreserved()
    {
        var explicitTimeout = TimeSpan.FromSeconds(1);
        var registrations = BuildRegistrations(checks => checks
            .AddCheck("explicit-timeout", () => HealthCheckResult.Healthy(), tags: null, timeout: explicitTimeout));

        registrations["explicit-timeout"].Timeout.ShouldBe(explicitTimeout);
    }

    [Fact]
    public void ChecksRegisteredAfterAddJoinHealthChecksAlsoGetTimeout()
    {
        // Чеки регистрируются из разных библиотек и в произвольном порядке относительно
        // AddJoinHealthChecks, поэтому дефолт должен проставляться после всех регистраций.
        var services = new ServiceCollection();
        _ = services.AddJoinHealthChecks();
        _ = services.AddHealthChecks().AddCheck("late", () => HealthCheckResult.Healthy());

        var registrations = ResolveRegistrations(services);

        registrations["late"].Timeout.ShouldBe(HealthCheckExtensions.DefaultHealthCheckTimeout);
    }

    private static Dictionary<string, HealthCheckRegistration> BuildRegistrations(
        Action<IHealthChecksBuilder> registerChecks)
    {
        var services = new ServiceCollection();
        registerChecks(services.AddHealthChecks());
        _ = services.AddJoinHealthChecks();

        return ResolveRegistrations(services);
    }

    private static Dictionary<string, HealthCheckRegistration> ResolveRegistrations(IServiceCollection services)
        => services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value
            .Registrations
            .ToDictionary(registration => registration.Name);
}
