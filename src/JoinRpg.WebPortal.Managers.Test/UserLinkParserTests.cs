using JoinRpg.WebPortal.Managers.Claims;

namespace JoinRpg.WebPortal.Managers.Test;

public class UserLinkParserTests
{
    [Theory]
    [InlineData("123", 123)]
    [InlineData("/users/123", 123)]
    [InlineData("https://joinrpg.ru/users/123", 123)]
    [InlineData("https://dev.joinrpg.ru/users/123", 123)]
    [InlineData("https://localhost:5001/users/123", 123)]
    [InlineData("http://joinrpg.ru/users/123", 123)]
    [InlineData("https://joinrpg.ru/users/123/", 123)]
    [InlineData(" 123 ", 123)]
    [InlineData(" /users/123 ", 123)]
    public void TryParseUserLink_ValidFormats_ReturnsTrue(string link, int expectedUserId)
    {
        var result = UserLinkParser.TryParseUserLink(link, out var userId);

        result.ShouldBeTrue();
        userId.ShouldBe(expectedUserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    [InlineData("users/123")]
    [InlineData("/users/abc")]
    [InlineData("https://joinrpg.ru/users/abc")]
    [InlineData("https://joinrpg.ru/users/123/extra")]
    [InlineData("/users/123/extra")]
    [InlineData("123.456")]
    [InlineData("-123")]
    [InlineData("0")]
    public void TryParseUserLink_InvalidFormats_ReturnsFalse(string? link)
    {
        var result = UserLinkParser.TryParseUserLink(link!, out var userId);

        result.ShouldBeFalse();
        userId.ShouldBe(0);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("/users/456", 456)]
    [InlineData("https://joinrpg.ru/users/789", 789)]
    public void ParseUserLink_ValidFormats_ReturnsUserId(string link, int expectedUserId)
    {
        var userId = UserLinkParser.ParseUserLink(link);

        userId.ShouldBe(expectedUserId);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("/users/abc")]
    [InlineData("")]
    public void ParseUserLink_InvalidFormats_ThrowsFormatException(string link)
    {
        Should.Throw<FormatException>(() => UserLinkParser.ParseUserLink(link));
    }
}
