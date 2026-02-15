using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.IdPortal.Test;

public class DiTest
{
    [Fact]
    public void Container_Should_Build()
    {
        var builder = WebApplication.CreateBuilder();
        builder.ConfigureServices();

        var provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();

        scope.ServiceProvider.ShouldNotBeNull();
    }
}
