using JoinRpg.Common.WebInfrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore.Authentication;

namespace JoinRpg.Mcp.Test;

/// <summary>
/// Схема MCP — не провайдер логина. SignInManager.GetExternalAuthenticationSchemesAsync
/// отдаёт все схемы с непустым DisplayName, поэтому схема с дефолтным именем вылезала на
/// /Account/Login кнопкой «войти через MCP», которая ничего не логинит (issue #4873).
/// </summary>
public class McpAuthenticationSchemeTests
{
    private static readonly JoinRpgHostNamesOptions HostNames = new()
    {
        MainHost = "dev.joinrpg.ru",
        IdHost = "devid.joinrpg.ru",
        KogdaIgraHost = "dev.kogda-igra.ru",
        RatingHost = "rating.bastilia.ru",
    };

    [Fact]
    public async Task McpScheme_HasNoDisplayName_SoItIsNotShownAsLoginButton()
    {
        var scheme = await GetMcpSchemeAsync();

        scheme.ShouldNotBeNull();
        scheme.DisplayName.ShouldBeNullOrEmpty();
    }

    private static async Task<AuthenticationScheme?> GetMcpSchemeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJoinMcp(
            new McpResourceOptions { ClientId = "portal-mcp", ClientSecret = "s3cret" },
            HostNames);

        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        return await schemes.GetSchemeAsync(McpAuthenticationDefaults.AuthenticationScheme);
    }
}
