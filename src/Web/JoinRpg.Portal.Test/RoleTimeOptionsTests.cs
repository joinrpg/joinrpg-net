using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Test;

/// <summary>
/// На проде робот задан строкой <c>RoleTime__RobotUserId=66633</c> в manifests/prod/.env.
/// </summary>
public class RoleTimeOptionsTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
{
    [Fact]
    public void RobotUserId_BindsFromConfiguration()
    {
        using var configured = factory.WithWebHostBuilder(b => b.UseSetting("RoleTime:RobotUserId", "66633"));
        configured.Services.GetRequiredService<IOptions<RoleTimeOptions>>().Value.RobotUserId
            .ShouldBe(new UserIdentification(66633));
    }

    [Fact]
    public void RobotUserId_IsNullWhenNotConfigured()
        => factory.Services.GetRequiredService<IOptions<RoleTimeOptions>>().Value.RobotUserId.ShouldBeNull();
}
