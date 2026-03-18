using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.Portal.Test;

public class PlotLegacyRedirectTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
{
    [Theory]
    [InlineData("1/plot/edit", "/1/plots/edit")]
    [InlineData("1/plot/edit?PlotFolderId=132", "/1/plots/edit?PlotFolderId=132")]
    [InlineData("1/plot/edit?PlotFolderId=132&foo=bar", "/1/plots/edit?PlotFolderId=132&foo=bar")]
    public async Task LegacyPlotUrlRedirectsPermanently(string requestPath, string expectedLocation)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync(requestPath);

        response.StatusCode.ShouldBe(HttpStatusCode.MovedPermanently);
        response.Headers.Location?.OriginalString.ShouldBe(expectedLocation);
    }
}
