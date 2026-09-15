using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol;

namespace JoinRpg.Mcp.Test;

/// <summary>
/// Главный MCP-специфичный риск (ADR012 §4): токен с урезанным списком проектов не должен
/// давать доступ к проектам вне claim <c>projects</c>, даже если сам пользователь мастер и там.
/// </summary>
public class McpAuthContextTests
{
    [Fact]
    public void IsProjectGranted_ProjectInClaim_ReturnsTrue()
    {
        var context = CreateContext("1,2,3");

        context.IsProjectGranted(2).ShouldBeTrue();
    }

    [Fact]
    public void IsProjectGranted_ProjectNotInClaim_ReturnsFalse()
    {
        var context = CreateContext("1,2,3");

        context.IsProjectGranted(999).ShouldBeFalse();
    }

    [Fact]
    public void IsProjectGranted_NoClaimAtAll_ReturnsFalse()
    {
        var context = CreateContext(claimValue: null);

        context.IsProjectGranted(1).ShouldBeFalse();
    }

    [Fact]
    public void EnsureProjectGranted_ProjectNotInClaim_ThrowsMcpException()
    {
        var context = CreateContext("1,2,3");

        Should.Throw<McpException>(() => context.EnsureProjectGranted(999));
    }

    [Fact]
    public void EnsureProjectGranted_ProjectInClaim_DoesNotThrow()
    {
        var context = CreateContext("1,2,3");

        Should.NotThrow(() => context.EnsureProjectGranted(1));
    }

    private static McpAuthContext CreateContext(string? claimValue)
    {
        var identity = claimValue is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity([new Claim("projects", claimValue)]);

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new FakeHttpContextAccessor { HttpContext = httpContext };

        return new McpAuthContext(accessor);
    }

    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
