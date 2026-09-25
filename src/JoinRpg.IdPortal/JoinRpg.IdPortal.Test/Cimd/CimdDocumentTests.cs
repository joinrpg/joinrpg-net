using JoinRpg.IdPortal.OAuthServer.Cimd;

namespace JoinRpg.IdPortal.Test.Cimd;

/// <summary>
/// Валидация самого документа (§4.1-4.2 драфта + обязательные поля по MCP).
/// </summary>
public class CimdDocumentTests
{
    private const string Url = "https://app.example.com/client.json";

    private static CimdClientId ClientId
    {
        get
        {
            CimdClientId.TryParse(Url, out var parsed).ShouldBeTrue();
            return parsed!;
        }
    }

    private static string Doc(string body) => body.Replace('\'', '"');

    [Fact]
    public void AcceptsMinimalValidDocument()
    {
        var result = CimdDocument.Validate(Doc($$"""
            {'client_id':'{{Url}}','client_name':'Example MCP Client',
             'redirect_uris':['http://127.0.0.1:3000/callback']}
            """), ClientId);

        result.IsValid.ShouldBeTrue(result.Error);
        result.Document!.ClientName.ShouldBe("Example MCP Client");
        result.Document.ValidRedirectUris.Count.ShouldBe(1);
    }

    [Fact]
    public void RejectsClientIdMismatch()
    {
        // §4.1: сравнение простое строковое, без нормализации — иначе чужой документ
        // можно было бы выдать за свой.
        var result = CimdDocument.Validate(Doc("""
            {'client_id':'https://evil.example.com/client.json','client_name':'X',
             'redirect_uris':['https://app.example.com/cb']}
            """), ClientId);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldContain("не совпадает");
    }

    [Theory]
    [InlineData("{'client_id':'URL','redirect_uris':['https://app.example.com/cb']}", "client_name")]
    [InlineData("{'client_id':'URL','client_name':'X'}", "redirect_uris")]
    public void RejectsMissingRequiredFields(string body, string expected)
    {
        var result = CimdDocument.Validate(Doc(body.Replace("URL", Url)), ClientId);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldContain(expected);
    }

    [Theory]
    [InlineData("'client_secret':'hunter2'")]
    [InlineData("'client_secret_expires_at':0")]
    public void RejectsSharedSecret(string extra)
    {
        var result = CimdDocument.Validate(Doc($$"""
            {'client_id':'{{Url}}','client_name':'X',{{extra}},
             'redirect_uris':['https://app.example.com/cb']}
            """), ClientId);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldContain("client_secret");
    }

    [Theory]
    [InlineData("client_secret_post")]
    [InlineData("client_secret_basic")]
    [InlineData("client_secret_jwt")]
    public void RejectsSymmetricAuthMethods(string method)
    {
        var result = CimdDocument.Validate(Doc($$"""
            {'client_id':'{{Url}}','client_name':'X','token_endpoint_auth_method':'{{method}}',
             'redirect_uris':['https://app.example.com/cb']}
            """), ClientId);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldContain("общем секрете");
    }

    [Fact]
    public void RejectsPlainHttpRedirectThatIsNotLoopback()
    {
        // MCP security considerations: redirect_uri — только https или loopback.
        var result = CimdDocument.Validate(Doc($$"""
            {'client_id':'{{Url}}','client_name':'X','redirect_uris':['http://evil.example.com/cb']}
            """), ClientId);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldContain("https");
    }

    [Fact]
    public void RejectsMalformedJson() =>
        CimdDocument.Validate("{ not json", ClientId).IsValid.ShouldBeFalse();

    [Fact]
    public void DetectsLoopbackOnly_ForConsentWarning()
    {
        var result = CimdDocument.Validate(Doc($$"""
            {'client_id':'{{Url}}','client_name':'X',
             'redirect_uris':['http://127.0.0.1:3000/callback','http://[::1]:3000/callback']}
            """), ClientId);

        result.IsValid.ShouldBeTrue(result.Error);
        result.Document!.IsLoopbackOnly.ShouldBeTrue();
    }

    [Fact]
    public void MixedRedirects_AreNotLoopbackOnly()
    {
        var result = CimdDocument.Validate(Doc($$"""
            {'client_id':'{{Url}}','client_name':'X',
             'redirect_uris':['http://127.0.0.1:3000/callback','https://app.example.com/cb']}
            """), ClientId);

        result.IsValid.ShouldBeTrue(result.Error);
        result.Document!.IsLoopbackOnly.ShouldBeFalse();
    }
}
