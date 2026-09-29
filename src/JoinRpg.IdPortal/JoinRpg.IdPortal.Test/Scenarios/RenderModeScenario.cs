using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

/// <summary>
/// Аватарка пользователя живёт в MainLayout, то есть попадает на каждую авторизованную
/// страницу. Пока на ней стоял @rendermode InteractiveServer, каждый просмотр страницы
/// поднимал circuit — из-за этого IdPortal нельзя было развернуть больше чем в один под
/// без sticky-сессий (manifests/base/id-portal-deployment.yaml).
/// Интерактива компоненту не нужно, поэтому он рендерится статически; тест сторожит, чтобы
/// render mode не вернули обратно.
/// </summary>
[Collection("IdPortal")]
public class RenderModeScenario(IdPortalApplicationFactory factory)
{
    /// <summary>Маркер, который Blazor вставляет в HTML для компонента с интерактивным render mode.</summary>
    private const string ServerComponentMarker = "\"type\":\"server\"";

    [Fact]
    public async Task Home_WhenAuthenticated_RendersAvatarWithoutServerCircuit()
    {
        var client = await LoginAsync();

        var response = await client.GetAsync("/");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();

        // Аватарка нарисовалась — значит статического рендера компоненту хватает.
        var doc = await response.AsHtmlDocument();
        doc.DocumentNode.SelectSingleNode("//div[@class='user-info']//img").ShouldNotBeNull();

        html.ShouldNotContain(ServerComponentMarker);
    }

    private async Task<HttpClient> LoginAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var loginPage = await client.GetAsync("/Account/Login");
        var doc = await loginPage.AsHtmlDocument();
        var fields = doc.GetFormFields();
        fields["Input.Email"] = IdPortalApplicationFactory.TestUserEmail;
        fields["Input.Password"] = IdPortalApplicationFactory.TestUserPassword;
        await client.PostAsync(doc.GetFormAction("/Account/Login"), new FormUrlEncodedContent(fields!));

        return client;
    }
}
