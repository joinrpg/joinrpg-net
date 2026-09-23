using JoinRpg.IdPortal.OAuthServer.Cimd;

namespace JoinRpg.IdPortal.Test.Cimd;

/// <summary>
/// Правила к client_id-URL из draft-ietf-oauth-client-id-metadata-document-00 §3.
/// Проверки чистые, без сети и без поднятия хоста.
/// </summary>
public class CimdClientIdTests
{
    [Theory]
    [InlineData("https://app.example.com/client.json")]
    [InlineData("https://app.example.com/oauth/client-metadata.json")]
    [InlineData("https://app.example.com:8443/client.json")] // порт разрешён
    public void Accepts(string clientId) =>
        CimdClientId.TryParse(clientId, out _).ShouldBeTrue();

    [Theory]
    [InlineData("http://app.example.com/client.json", "схема должна быть https")]
    [InlineData("https://app.example.com", "path обязателен")]
    [InlineData("https://app.example.com/", "пустой path не считается")]
    [InlineData("https://app.example.com/a/../client.json", "сегменты .. запрещены")]
    [InlineData("https://app.example.com/./client.json", "сегменты . запрещены")]
    [InlineData("https://app.example.com/client.json#frag", "fragment запрещён")]
    [InlineData("https://user:pw@app.example.com/client.json", "userinfo запрещён")]
    [InlineData("https://app.example.com/client.json?x=1", "query не допускается")]
    [InlineData("integration-test-client", "обычный client_id — не CIMD")]
    [InlineData("", "пустая строка")]
    [InlineData(null, "null")]
    public void Rejects(string? clientId, string why) =>
        CimdClientId.TryParse(clientId, out _).ShouldBeFalse(why);

    [Fact]
    public void ExposesHostForConsentScreen()
    {
        CimdClientId.TryParse("https://app.example.com/client.json", out var parsed).ShouldBeTrue();

        parsed!.Host.ShouldBe("app.example.com");
    }

    [Fact]
    public void LooksLikeCimd_LeavesOrdinaryClientIdsAlone() =>
        CimdClientId.LooksLikeCimd("integration-test-client").ShouldBeFalse();
}
