using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

[Collection("IdPortal")]
public class LoginScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task Login_WithValidCredentials_Succeeds()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.StatusCode.ShouldBe(HttpStatusCode.OK);

        var doc = await loginPage.AsHtmlDocument();
        var fields = doc.GetFormFields();
        fields["Input.Email"] = IdPortalApplicationFactory.TestUserEmail;
        fields["Input.Password"] = IdPortalApplicationFactory.TestUserPassword;

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(fields!));

        // After successful login, should land on /
        loginResponse.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/");
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(Skip = "TODO: fix")]
    public async Task Login_WithWrongPassword_ShowsError()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var loginPage = await client.GetAsync("/Account/Login");
        var doc = await loginPage.AsHtmlDocument();
        var fields = doc.GetFormFields();
        fields["Input.Email"] = IdPortalApplicationFactory.TestUserEmail;
        fields["Input.Password"] = "WrongPassword!";

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(fields!));

        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var responseDoc = await loginResponse.AsHtmlDocument();
        responseDoc.DocumentNode.InnerHtml.ShouldContain("Error", Case.Insensitive);
    }

    [Fact(Skip = "TODO: fix - needs unconfirmed user creation without InMemoryUserStore")]
    public async Task Login_WithUnconfirmedEmail_ShowsError()
    {
        // TODO: Create unconfirmed user via real UserManager or DB directly
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var loginPage = await client.GetAsync("/Account/Login");
        var doc = await loginPage.AsHtmlDocument();
        var fields = doc.GetFormFields();
        fields["Input.Email"] = "unconfirmed@joinrpg.ru";
        fields["Input.Password"] = "TestPassword123!";

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(fields!));

        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        loginResponse.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/Account/Login");
    }
}
