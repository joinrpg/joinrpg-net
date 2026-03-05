using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

[Collection("IdPortal")]
public class ForgotPasswordScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task ForgotPassword_WithKnownEmail_SendsEmail()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var page = await client.GetAsync("/Account/ForgotPassword");
        page.StatusCode.ShouldBe(HttpStatusCode.OK);

        var doc = await page.AsHtmlDocument();
        var fields = doc.GetFormFields();
        fields["Input.Email"] = IdPortalApplicationFactory.TestUserEmail;

        var response = await client.PostAsync("/Account/ForgotPassword", new FormUrlEncodedContent(fields!));

        // Redirects to confirmation page
        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location?.ToString().ShouldContain("ForgotPasswordConfirmation");

        // Email service should have captured the reset URL
        factory.CaptureEmail.LastResetPasswordUrl.ShouldNotBeNull();
        factory.CaptureEmail.LastResetPasswordUrl.ShouldContain("ResetPassword");
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesPassword()
    {
        const string newPassword = "NewPassword456!";

        // Step 1: Request password reset for dedicated reset user
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var forgotPage = await client.GetAsync("/Account/ForgotPassword");
        var doc = await forgotPage.AsHtmlDocument();
        var forgotFields = doc.GetFormFields();
        forgotFields["Input.Email"] = IdPortalApplicationFactory.TestResetUserEmail;

        await client.PostAsync(doc.GetFormAction("/Account/ForgotPassword"), new FormUrlEncodedContent(forgotFields!));

        var resetUrl = factory.CaptureEmail.LastResetPasswordUrl;
        resetUrl.ShouldNotBeNull();

        // Step 2: Extract code from reset URL and submit new password
        var uri = new Uri(resetUrl);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var code = queryParams["code"];
        code.ShouldNotBeNull();

        var resetPageUrl = $"/Account/ResetPassword?code={Uri.EscapeDataString(code)}";
        var resetPage = await client.GetAsync(resetPageUrl);
        resetPage.StatusCode.ShouldBe(HttpStatusCode.OK);

        var resetDoc = await resetPage.AsHtmlDocument();
        var resetFields = resetDoc.GetFormFields();
        resetFields["Input.Email"] = IdPortalApplicationFactory.TestResetUserEmail;
        resetFields["Input.Password"] = newPassword;
        resetFields["Input.ConfirmPassword"] = newPassword;

        var resetResponse = await client.PostAsync(resetDoc.GetFormAction(resetPageUrl), new FormUrlEncodedContent(resetFields!));

        // Should redirect to confirmation
        resetResponse.StatusCode.ShouldBe(HttpStatusCode.Found);
        resetResponse.Headers.Location?.ToString().ShouldContain("ResetPasswordConfirmation");

        // Step 3: Login with new password should succeed
        var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        var loginPage = await loginClient.GetAsync("/Account/Login");
        var loginDoc = await loginPage.AsHtmlDocument();
        var loginFields = loginDoc.GetFormFields();
        loginFields["Input.Email"] = IdPortalApplicationFactory.TestResetUserEmail;
        loginFields["Input.Password"] = newPassword;

        var loginResponse = await loginClient.PostAsync(loginDoc.GetFormAction("/Account/Login"), new FormUrlEncodedContent(loginFields!));
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        loginResponse.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/");
    }
}
