using System.Text.Json;

namespace JoinRpg.IdPortal.Test.Scenarios;

/// <summary>
/// ADR012 §3 требует PKCE именно с S256. <c>RequireProofKeyForCodeExchange()</c> требует PKCE,
/// но метод не ограничивает, поэтому сервер рекламировал ещё и <c>plain</c> — а при нём
/// code_verifier едет в authorize-запросе открытым текстом, и защита от перехвата кода почти
/// нулевая. Обнаружено на живом dev при попытке подключить Claude Code.
/// </summary>
[Collection("IdPortal")]
public class OAuthPkceScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task Discovery_AdvertisesS256_ButNotPlain()
    {
        var client = factory.CreateClient();

        var discovery = await client.GetFromJsonAsync<Dictionary<string, JsonElement>>(
            "/.well-known/openid-configuration");

        discovery.ShouldNotBeNull();

        var methods = discovery["code_challenge_methods_supported"]
            .EnumerateArray().Select(x => x.GetString()).ToList();

        methods.ShouldContain("S256");
        methods.ShouldNotContain("plain");
    }
}
