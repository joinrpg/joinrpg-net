using JoinRpg.WebPortal.Managers.Claims;

namespace JoinRpg.WebPortal.Managers.Test;

public class UserLinkParserTests
{
    [Theory]
    [InlineData("https://vk.com/id123456", "id123456")]
    [InlineData("https://vk.ru/id123456", "id123456")]
    [InlineData("vk.com/id123456", "id123456")]
    [InlineData("https://vk.com/username", "username")]
    [InlineData("vk.com/username/", "username")]
    public void TryParseSocialUserLink_VkFormats_ReturnsVkProfile(string link, string expectedVkId)
    {
        UserLinkParser.TryParseSocialUserLink(link, out var result).ShouldBeTrue();
        result.ShouldBeOfType<ParsedUserLink.VkProfile>()
            .VkId.ShouldBe(expectedVkId);
    }

    [Theory]
    [InlineData("@username", "username")]
    [InlineData("https://t.me/username", "username")]
    [InlineData("t.me/username", "username")]
    [InlineData("t.me/username/", "username")]
    public void TryParseSocialUserLink_TelegramFormats_ReturnsTelegramProfile(string link, string expectedUsername)
    {
        UserLinkParser.TryParseSocialUserLink(link, out var result).ShouldBeTrue();
        result.ShouldBeOfType<ParsedUserLink.TelegramProfile>()
            .Username.ShouldBe(expectedUsername);
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.org")]
    public void TryParseSocialUserLink_EmailFormats_ReturnsEmailAddress(string link)
    {
        UserLinkParser.TryParseSocialUserLink(link, out var result).ShouldBeTrue();
        result.ShouldBeOfType<ParsedUserLink.EmailAddress>()
            .Email.ShouldBe(link);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("/user/123", 123)]
    [InlineData("https://joinrpg.ru/user/123", 123)]
    public void TryParseSocialUserLink_JoinRpgFormats_ReturnsJoinRpgUser(string link, int expectedId)
    {
        UserLinkParser.TryParseSocialUserLink(link, out var result).ShouldBeTrue();
        result.ShouldBeOfType<ParsedUserLink.JoinRpgUser>()
            .UserId.Value.ShouldBe(expectedId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    public void TryParseSocialUserLink_InvalidFormats_ReturnsFalse(string link)
    {
        UserLinkParser.TryParseSocialUserLink(link, out var result).ShouldBeFalse();
        result.ShouldBeNull();
    }


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
