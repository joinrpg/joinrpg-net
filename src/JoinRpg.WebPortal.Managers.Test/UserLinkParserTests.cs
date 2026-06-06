using JoinRpg.WebPortal.Managers.Claims;

namespace JoinRpg.WebPortal.Managers.Test;

public class UserLinkParserTests
{
    [Theory]
    [InlineData("123", 123)]
    [InlineData("/user/123", 123)]
    [InlineData("https://joinrpg.ru/user/123", 123)]
    [InlineData("https://dev.joinrpg.ru/user/123", 123)]
    [InlineData("https://localhost:5001/user/123", 123)]
    [InlineData("http://joinrpg.ru/user/123", 123)]
    [InlineData("https://joinrpg.ru/user/123/", 123)]
    [InlineData(" 123 ", 123)]
    [InlineData(" /user/123 ", 123)]
    public void TryParseUserLink_ValidFormats_ReturnsTrue(string link, int expectedUserId)
    {
        var userId = UserLinkParser.ParseUserLink(link);

        userId.Value.ShouldBe(expectedUserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    [InlineData("user/123")]
    [InlineData("/user/abc")]
    [InlineData("https://joinrpg.ru/user/abc")]
    [InlineData("https://joinrpg.ru/user/123/extra")]
    [InlineData("/user/123/extra")]
    [InlineData("/users/123")]
    [InlineData("https://joinrpg.ru/users/123")]
    [InlineData("123.456")]
    [InlineData("-123")]
    [InlineData("0")]
    public void TryParseUserLink_InvalidFormats_ReturnsFalse(string? link)
    {
        var result = UserLinkParser.TryParseUserLink(link!, out var _);

        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("/user/456", 456)]
    [InlineData("https://joinrpg.ru/user/789", 789)]
    public void ParseUserLink_ValidFormats_ReturnsUserId(string link, int expectedUserId)
    {
        var userId = UserLinkParser.ParseUserLink(link);

        userId.Value.ShouldBe(expectedUserId);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("/user/abc")]
    [InlineData("")]
    public void ParseUserLink_InvalidFormats_ThrowsFormatException(string link)
    {
        Should.Throw<FormatException>(() => UserLinkParser.ParseUserLink(link));
    }
}
