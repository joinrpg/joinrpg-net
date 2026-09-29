using Microsoft.Extensions.Primitives;

namespace JoinRpg.IdPortal.OAuthServer.Test;

public class OAuthConsentTest
{
    [Fact]
    public void ParseProjectIds_SingleCommaSeparatedValue_Parsed()
        => OAuthConsent.ParseProjectIds(new StringValues("123,456")).ShouldBe([123, 456]);

    /// <summary>
    /// Страница согласия — обычная HTML-форма, и каждый отмеченный чекбокс уезжает
    /// отдельным projects=N, то есть в query приходит несколько значений.
    /// </summary>
    [Fact]
    public void ParseProjectIds_RepeatedValues_Parsed()
        => OAuthConsent.ParseProjectIds(new StringValues(["123", "456"])).ShouldBe([123, 456]);

    [Fact]
    public void ParseProjectIds_NoValues_Empty()
        => OAuthConsent.ParseProjectIds(StringValues.Empty).ShouldBeEmpty();
}
