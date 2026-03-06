using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

[Collection("IdPortal")]
public class ProfileScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task Home_WhenNotAuthenticated_RedirectsToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location?.ToString().ShouldContain("Login");
    }

    [Fact(Skip = "Тест пока не дописан")]
    public async Task Home_WhenAuthenticated_ShowsDisplayName()
    {
        // Login first
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var loginPage = await client.GetAsync("/Account/Login");
        var doc = await loginPage.AsHtmlDocument();
        var fields = doc.GetFormFields();
        fields["Input.Email"] = IdPortalApplicationFactory.TestUserEmail;
        fields["Input.Password"] = IdPortalApplicationFactory.TestUserPassword;

        var loginResponse = await client.PostAsync(doc.GetFormAction("/Account/Login"), new FormUrlEncodedContent(fields!));
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        loginResponse.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/");

        // Check home page shows display name
        var homeResponse = await client.GetAsync("/");
        homeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var homeDoc = await homeResponse.AsHtmlDocument();
        homeDoc.DocumentNode.InnerText.ShouldContain(IdPortalApplicationFactory.TestDisplayName);
    }
}
