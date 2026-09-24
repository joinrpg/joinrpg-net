using System.Net;
using JoinRpg.Portal.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

/// <summary>
/// Регрессия на #4871: при двух заявках на один проект <see cref="ClaimController.MyClaim"/>
/// не может выбрать одну и редиректит в список своих заявок. Если этот экшн не резолвится,
/// редирект падает с InvalidOperationException и игрок видит 500.
/// </summary>
public class MyClaimListRedirectTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
{
    [Fact]
    public void MyClaimListActionIsResolvable()
    {
        var linkGenerator = factory.Services.GetRequiredService<LinkGenerator>();

        var path = linkGenerator.GetPathByAction(
            nameof(MyClaimListController.My),
            "MyClaimList");

        path.ShouldBe("/my/claims");
    }

    [Theory]
    [InlineData("claimlist/my", "/my/claims")]
    public async Task LegacyClaimListUrlRedirectsPermanently(string requestPath, string expectedLocation)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync(requestPath);

        response.StatusCode.ShouldBe(HttpStatusCode.MovedPermanently);
        response.Headers.Location?.OriginalString.ShouldBe(expectedLocation);
    }
}
